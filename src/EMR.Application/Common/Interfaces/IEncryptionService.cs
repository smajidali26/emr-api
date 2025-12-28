namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive PHI data
/// HIPAA requires encryption at rest for Protected Health Information
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts sensitive data using AES-256-GCM
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64 encoded encrypted data</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts AES-256-GCM encrypted data
    /// </summary>
    /// <param name="encryptedText">Base64 encoded encrypted data</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Check if a value is already encrypted
    /// </summary>
    bool IsEncrypted(string value);
}
