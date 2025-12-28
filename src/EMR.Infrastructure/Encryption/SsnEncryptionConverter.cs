using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Encryption;

/// <summary>
/// EF Core value converter for SSN encryption using Azure Key Vault
/// SECURITY FIX: Task #1 - Implement SSN Encryption (Emily Wang - 16h)
/// SECURITY FIX: Task #2633 - Fixed key management and audit logging (Code Review)
/// HIPAA Compliance: Encrypts SSN at rest using AES-256-GCM with keys managed in Azure Key Vault
/// </summary>
public class SsnEncryptionConverter : ValueConverter<string?, string?>
{
    private static readonly Lazy<EncryptionService> _encryptionService = new(() => new EncryptionService());

    // HIPAA FIX: AsyncLocal to capture operation context for audit logging
    // This allows passing patient/correlation context through EF Core operations
    private static readonly AsyncLocal<AuditContext?> _auditContext = new();

    /// <summary>
    /// Set the audit context before performing database operations
    /// Call this before SaveChanges to provide patient context for HIPAA audit logs
    /// </summary>
    public static void SetAuditContext(string? patientId, string? correlationId, string? userId)
    {
        _auditContext.Value = new AuditContext(patientId, correlationId, userId);
    }

    /// <summary>
    /// Clear the audit context after database operations complete
    /// </summary>
    public static void ClearAuditContext()
    {
        _auditContext.Value = null;
    }

    public SsnEncryptionConverter()
        : base(
            plaintext => Encrypt(plaintext),
            ciphertext => Decrypt(ciphertext))
    {
    }

    private static string? Encrypt(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            return null;

        var result = _encryptionService.Value.Encrypt(plaintext);
        // SECURITY FIX: Audit log encryption operations (HIPAA requirement)
        var context = _auditContext.Value;
        _encryptionService.Value.LogAuditEvent("SSN_ENCRYPT", success: true, context);
        return result;
    }

    private static string? Decrypt(string? ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
            return null;

        var result = _encryptionService.Value.Decrypt(ciphertext);
        // SECURITY FIX: Audit log decryption operations (HIPAA requirement)
        var context = _auditContext.Value;
        _encryptionService.Value.LogAuditEvent("SSN_DECRYPT", success: true, context);
        return result;
    }

    /// <summary>
    /// Audit context for HIPAA-compliant logging
    /// </summary>
    private record AuditContext(string? PatientId, string? CorrelationId, string? UserId);

    /// <summary>
    /// Internal encryption service using AES-256-GCM with Azure Key Vault key management
    /// Thread-safe singleton pattern for key caching
    /// SECURITY FIX: Task #2633 - Proper logger initialization and audit logging
    /// ARCHITECTURE FIX: Use static logger to prevent disposal issues
    /// </summary>
    private class EncryptionService
    {
        private readonly byte[] _encryptionKey;
        private const int KeySize = 32; // 256 bits for AES-256
        private const int NonceSize = 12; // 96 bits for GCM
        private const int TagSize = 16; // 128 bits authentication tag

        // ARCHITECTURE FIX: Static logger factory and logger to prevent disposal
        // The factory is kept alive for the application lifetime
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information); // Allow info for audit logs
        });
        private static readonly ILogger<EncryptionService> _logger = _loggerFactory.CreateLogger<EncryptionService>();

        // SECURITY FIX: Static flag to track if using development key (for audit)
        private static bool _usingDevelopmentKey = false;

        // SECURITY FIX: Store development key persistently to avoid regeneration issues
        private static byte[]? _cachedDevelopmentKey = null;
        private static readonly object _keyLock = new object();

        public EncryptionService()
        {
            try
            {
                // Attempt to retrieve encryption key from Azure Key Vault
                _encryptionKey = GetEncryptionKeyFromKeyVault();
                _usingDevelopmentKey = false;
            }
            catch (Exception ex)
            {
                // FALLBACK: For development/testing environments without Key Vault access
                // In production, this should fail fast and prevent application startup
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

                if (!isDevelopment)
                {
                    _logger.LogCritical(ex,
                        "CRITICAL SECURITY ERROR: Cannot initialize SSN encryption. " +
                        "Azure Key Vault is not accessible. Application startup blocked for HIPAA compliance.");
                    throw new InvalidOperationException(
                        "CRITICAL SECURITY ERROR: Cannot initialize SSN encryption. Azure Key Vault is not accessible. " +
                        "SSN encryption is required for HIPAA compliance. Application cannot start.", ex);
                }

                // Development fallback - use environment-specific key
                // WARNING: This should NEVER be used in production
                _logger.LogWarning(
                    "DEVELOPMENT MODE: Using fallback encryption key. This is NOT secure for production use. " +
                    "Configure Azure Key Vault for production deployments. Error: {Error}", ex.Message);

                _encryptionKey = GetDevelopmentFallbackKey();
                _usingDevelopmentKey = true;
            }
        }

        /// <summary>
        /// Log audit event for HIPAA compliance
        /// SECURITY FIX: Task #2633 - Add audit logging for encryption operations
        /// HIPAA FIX: Include patient context for complete audit trail
        /// </summary>
        public void LogAuditEvent(string operation, bool success, AuditContext? context)
        {
            var keySource = _usingDevelopmentKey ? "DEV" : "KV"; // Shortened for logs
            var patientId = context?.PatientId ?? "UNKNOWN";
            var userId = context?.UserId ?? "SYSTEM";
            var correlationId = context?.CorrelationId ?? Guid.NewGuid().ToString("N")[..8];

            if (success)
            {
                _logger.LogInformation(
                    "AUDIT: SSN {Operation} | Patient: {PatientId} | User: {UserId} | " +
                    "CorrelationId: {CorrelationId} | KeySource: {KeySource} | Timestamp: {Timestamp}",
                    operation, patientId, userId, correlationId, keySource, DateTime.UtcNow.ToString("O"));
            }
            else
            {
                _logger.LogWarning(
                    "AUDIT: SSN {Operation} FAILED | Patient: {PatientId} | User: {UserId} | " +
                    "CorrelationId: {CorrelationId} | KeySource: {KeySource} | Timestamp: {Timestamp}",
                    operation, patientId, userId, correlationId, keySource, DateTime.UtcNow.ToString("O"));
            }
        }

        /// <summary>
        /// Retrieve the encryption key from Azure Key Vault
        /// SECURITY: Production key management using Azure Key Vault
        /// </summary>
        private byte[] GetEncryptionKeyFromKeyVault()
        {
            // Get Key Vault URL from configuration
            var keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL")
                ?? throw new InvalidOperationException(
                    "Azure Key Vault URL not configured. Set AZURE_KEYVAULT_URL environment variable.");

            var secretName = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_SSN_KEY_NAME") ?? "SSN-Encryption-Key";

            try
            {
                // Use DefaultAzureCredential for authentication
                // This supports multiple authentication methods in order:
                // 1. Environment variables (for local dev with service principal)
                // 2. Managed Identity (for Azure-hosted apps)
                // 3. Visual Studio / Azure CLI / Azure PowerShell (for local dev)
                var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

                // Retrieve the encryption key from Key Vault
                KeyVaultSecret secret = client.GetSecret(secretName);

                // The secret value should be a base64-encoded 256-bit key
                var keyBase64 = secret.Value;
                var key = Convert.FromBase64String(keyBase64);

                if (key.Length != KeySize)
                {
                    throw new InvalidOperationException(
                        $"Invalid encryption key size in Key Vault. Expected {KeySize} bytes, got {key.Length} bytes. " +
                        $"The key must be a {KeySize * 8}-bit (32-byte) AES key encoded as base64.");
                }

                return key;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to retrieve SSN encryption key from Azure Key Vault '{keyVaultUrl}'. " +
                    "Ensure the Key Vault is accessible and contains a secret named '{secretName}' with a valid AES-256 key.", ex);
            }
        }

        /// <summary>
        /// Generate a development-only fallback key
        /// SECURITY FIX: Task #2633 - Improved key generation with random entropy
        /// WARNING: This must NEVER be used in production
        /// </summary>
        private byte[] GetDevelopmentFallbackKey()
        {
            // Thread-safe check for cached key
            lock (_keyLock)
            {
                if (_cachedDevelopmentKey != null)
                {
                    return _cachedDevelopmentKey;
                }

                // Try to load existing development key from local file
                var keyFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EMR", "dev-encryption.key");

                try
                {
                    if (File.Exists(keyFilePath))
                    {
                        var existingKey = Convert.FromBase64String(File.ReadAllText(keyFilePath));
                        if (existingKey.Length == KeySize)
                        {
                            _cachedDevelopmentKey = existingKey;
                            _logger.LogWarning(
                                "DEVELOPMENT: Loaded existing development encryption key from {Path}. " +
                                "This key is NOT suitable for production use.", keyFilePath);
                            return _cachedDevelopmentKey;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load existing development key, generating new one.");
                }

                // Generate a new random key with proper entropy
                // SECURITY FIX: Use RandomNumberGenerator instead of deterministic derivation
                _cachedDevelopmentKey = new byte[KeySize];
                RandomNumberGenerator.Fill(_cachedDevelopmentKey);

                // Save the key for persistence across restarts (development only)
                try
                {
                    var directory = Path.GetDirectoryName(keyFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllText(keyFilePath, Convert.ToBase64String(_cachedDevelopmentKey));
                    _logger.LogWarning(
                        "DEVELOPMENT: Generated new development encryption key and saved to {Path}. " +
                        "This key is NOT suitable for production use.", keyFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Could not persist development key. Key will be regenerated on restart, " +
                        "which may cause decryption failures for previously encrypted data.");
                }

                return _cachedDevelopmentKey;
            }
        }

        /// <summary>
        /// Encrypt plaintext using AES-256-GCM
        /// </summary>
        public string Encrypt(string plaintext)
        {
            // Generate a random nonce for this encryption operation
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Convert plaintext to bytes
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            // Allocate buffer for ciphertext and tag
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagSize];

            // Encrypt using AES-GCM
            using var aesGcm = new AesGcm(_encryptionKey, TagSize);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine nonce + tag + ciphertext for storage
            // Format: [12 bytes nonce][16 bytes tag][variable length ciphertext]
            byte[] result = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

            // Return as base64 for storage in database
            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypt ciphertext using AES-256-GCM
        /// </summary>
        public string Decrypt(string ciphertextBase64)
        {
            try
            {
                // Decode from base64
                byte[] encryptedData = Convert.FromBase64String(ciphertextBase64);

                // Validate minimum length
                if (encryptedData.Length < NonceSize + TagSize)
                {
                    throw new CryptographicException("Invalid encrypted data: too short");
                }

                // Extract nonce, tag, and ciphertext
                byte[] nonce = new byte[NonceSize];
                byte[] tag = new byte[TagSize];
                byte[] ciphertext = new byte[encryptedData.Length - NonceSize - TagSize];

                Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
                Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
                Buffer.BlockCopy(encryptedData, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

                // Decrypt using AES-GCM
                byte[] plaintext = new byte[ciphertext.Length];
                using var aesGcm = new AesGcm(_encryptionKey, TagSize);
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

                // Convert back to string
                return Encoding.UTF8.GetString(plaintext);
            }
            catch (Exception ex)
            {
                // Log the error but don't expose encryption details
                _logger?.LogError(ex, "SSN decryption failed. Data may be corrupted or tampered with.");
                throw new CryptographicException("Failed to decrypt SSN. The data may be corrupted or the encryption key may have changed.", ex);
            }
        }
    }
}
