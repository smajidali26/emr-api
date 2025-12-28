using System.Security.Cryptography;
using System.Text;
using EMR.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Security;

/// <summary>
/// Service for encrypting and decrypting sensitive PHI data (SSN, etc.)
/// Uses AES-256-GCM for HIPAA-compliant encryption at rest
/// SECURITY FIX: Implement column-level encryption for PHI fields
/// Assigned: Anita Singh (Backend Team)
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<EncryptionService> _logger;
    private const int NonceSize = 12; // 96 bits for AES-GCM
    private const int TagSize = 16;   // 128 bits for authentication tag

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger;

        // Get encryption key from Azure Key Vault or configuration
        var keyBase64 = configuration["Encryption:Key"]
            ?? Environment.GetEnvironmentVariable("EMR_ENCRYPTION_KEY");

        if (string.IsNullOrEmpty(keyBase64))
        {
            // In development, generate a warning and use a derived key
            // In production, this should fail
            var environment = configuration["ApplicationSettings:Environment"] ?? "Development";
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "SECURITY ERROR: Encryption key is not configured. " +
                    "Set the EMR_ENCRYPTION_KEY environment variable or Encryption:Key in Azure Key Vault.");
            }

            _logger.LogWarning(
                "SECURITY WARNING: Using derived encryption key in non-production environment. " +
                "Ensure proper key management in production.");

            // Derive a key for development (NOT for production use)
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes("EMR_DEV_KEY_DO_NOT_USE_IN_PRODUCTION"));
        }
        else
        {
            _key = Convert.FromBase64String(keyBase64);

            if (_key.Length != 32) // 256 bits
            {
                throw new InvalidOperationException(
                    "SECURITY ERROR: Encryption key must be exactly 256 bits (32 bytes) for AES-256.");
            }
        }
    }

    /// <summary>
    /// Encrypts sensitive data using AES-256-GCM
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64 encoded encrypted data (nonce + ciphertext + tag)</returns>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aesGcm = new AesGcm(_key, TagSize);

            // Generate random nonce
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Prepare buffers
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            // Encrypt
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Combine nonce + ciphertext + tag for storage
            var result = new byte[NonceSize + cipherBytes.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(cipherBytes, 0, result, NonceSize, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + cipherBytes.Length, TagSize);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt sensitive data");
            throw new InvalidOperationException("Encryption failed. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Decrypts AES-256-GCM encrypted data
    /// </summary>
    /// <param name="encryptedText">Base64 encoded encrypted data</param>
    /// <returns>Decrypted plain text</returns>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);

            if (encryptedBytes.Length < NonceSize + TagSize)
            {
                throw new InvalidOperationException("Invalid encrypted data format");
            }

            // Extract nonce, ciphertext, and tag
            var nonce = new byte[NonceSize];
            var cipherLength = encryptedBytes.Length - NonceSize - TagSize;
            var cipherBytes = new byte[cipherLength];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedBytes, NonceSize, cipherBytes, 0, cipherLength);
            Buffer.BlockCopy(encryptedBytes, NonceSize + cipherLength, tag, 0, TagSize);

            // Decrypt
            using var aesGcm = new AesGcm(_key, TagSize);
            var plainBytes = new byte[cipherLength];
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed - possible data tampering or wrong key");
            throw new InvalidOperationException("Decryption failed. Data may be corrupted or tampered with.", ex);
        }
        catch (FormatException ex)
        {
            // This might be unencrypted legacy data - log and return as-is
            _logger.LogWarning(ex, "Data appears to be in legacy unencrypted format");
            return encryptedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt sensitive data");
            throw new InvalidOperationException("Decryption failed. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Check if a value is already encrypted (starts with valid base64 and correct length)
    /// </summary>
    public bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length >= NonceSize + TagSize;
        }
        catch
        {
            return false;
        }
    }
}
