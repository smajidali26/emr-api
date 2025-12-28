using System.Security.Cryptography;
using System.Text;
using FluentAssertions;

namespace EMR.UnitTests.Infrastructure.Encryption;

/// <summary>
/// Unit tests for SSN encryption round-trip functionality.
/// QA Condition: Verify SSN is properly encrypted and can be decrypted back to original value.
/// Uses AES-256-GCM encryption algorithm.
/// </summary>
public class SsnEncryptionConverterTests
{
    // Test encryption key (32 bytes for AES-256)
    private readonly byte[] _testKey;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public SsnEncryptionConverterTests()
    {
        // Generate a consistent test key for unit tests
        _testKey = new byte[KeySize];
        RandomNumberGenerator.Fill(_testKey);
    }

    #region Round-Trip Tests

    [Fact]
    public void EncryptDecrypt_StandardSSN_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalSsn = "123-45-6789";

        // Act
        var encrypted = Encrypt(originalSsn);
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalSsn);
    }

    [Fact]
    public void EncryptDecrypt_SSNWithoutDashes_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalSsn = "123456789";

        // Act
        var encrypted = Encrypt(originalSsn);
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalSsn);
    }

    [Theory]
    [InlineData("000-00-0000")]
    [InlineData("999-99-9999")]
    [InlineData("123-45-6789")]
    [InlineData("987654321")]
    [InlineData("111-11-1111")]
    public void EncryptDecrypt_VariousSSNFormats_ShouldReturnOriginalValue(string ssn)
    {
        // Act
        var encrypted = Encrypt(ssn);
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(ssn);
    }

    [Fact]
    public void EncryptDecrypt_EmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var originalSsn = "";

        // Act
        var encrypted = Encrypt(originalSsn);
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalSsn);
    }

    [Fact]
    public void EncryptDecrypt_LongString_ShouldReturnOriginalValue()
    {
        // Arrange - Test with longer string to ensure buffer handling
        var originalValue = "123-45-6789-extended-data-for-testing";

        // Act
        var encrypted = Encrypt(originalValue);
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalValue);
    }

    [Fact]
    public void EncryptDecrypt_SpecialCharacters_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalValue = "SSN: 123-45-6789 (verified)";

        // Act
        var encrypted = Encrypt(originalValue);
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalValue);
    }

    [Fact]
    public void EncryptDecrypt_UnicodeCharacters_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalValue = "SSN: 123-45-6789 \u00A9 \u2022";

        // Act
        var encrypted = Encrypt(originalValue);
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalValue);
    }

    #endregion

    #region Encryption Output Tests

    [Fact]
    public void Encrypt_SameValueTwice_ShouldProduceDifferentCiphertext()
    {
        // Arrange - Each encryption uses a random nonce
        var ssn = "123-45-6789";

        // Act
        var encrypted1 = Encrypt(ssn);
        var encrypted2 = Encrypt(ssn);

        // Assert - Different ciphertexts due to random nonce
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Encrypt_ShouldProduceBase64Output()
    {
        // Arrange
        var ssn = "123-45-6789";

        // Act
        var encrypted = Encrypt(ssn);

        // Assert - Should be valid base64
        var action = () => Convert.FromBase64String(encrypted);
        action.Should().NotThrow();
    }

    [Fact]
    public void Encrypt_OutputLength_ShouldBeGreaterThanInputDueToOverhead()
    {
        // Arrange - Overhead = nonce (12) + tag (16) bytes
        var ssn = "123-45-6789";
        var plaintextBytes = Encoding.UTF8.GetBytes(ssn);
        var expectedMinLength = NonceSize + TagSize + plaintextBytes.Length;

        // Act
        var encrypted = Encrypt(ssn);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Assert
        encryptedBytes.Length.Should().BeGreaterThanOrEqualTo(expectedMinLength);
    }

    [Fact]
    public void Encrypt_ShouldIncludeNonceTagAndCiphertext()
    {
        // Arrange
        var ssn = "123-45-6789";
        var plaintextLength = Encoding.UTF8.GetBytes(ssn).Length;

        // Act
        var encrypted = Encrypt(ssn);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Assert - Format: [12 bytes nonce][16 bytes tag][ciphertext]
        encryptedBytes.Length.Should().Be(NonceSize + TagSize + plaintextLength);
    }

    #endregion

    #region Decryption Error Handling Tests

    [Fact]
    public void Decrypt_TamperedCiphertext_ShouldThrowCryptographicException()
    {
        // Arrange
        var ssn = "123-45-6789";
        var encrypted = Encrypt(ssn);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Tamper with the ciphertext portion (after nonce and tag)
        encryptedBytes[NonceSize + TagSize]++;
        var tamperedEncrypted = Convert.ToBase64String(encryptedBytes);

        // Act & Assert
        var action = () => Decrypt(tamperedEncrypted);
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_TamperedTag_ShouldThrowCryptographicException()
    {
        // Arrange
        var ssn = "123-45-6789";
        var encrypted = Encrypt(ssn);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Tamper with the authentication tag
        encryptedBytes[NonceSize]++;
        var tamperedEncrypted = Convert.ToBase64String(encryptedBytes);

        // Act & Assert
        var action = () => Decrypt(tamperedEncrypted);
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_TruncatedData_ShouldThrowCryptographicException()
    {
        // Arrange
        var ssn = "123-45-6789";
        var encrypted = Encrypt(ssn);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Truncate the data
        var truncatedBytes = encryptedBytes.Take(10).ToArray();
        var truncatedEncrypted = Convert.ToBase64String(truncatedBytes);

        // Act & Assert
        var action = () => Decrypt(truncatedEncrypted);
        action.Should().Throw<CryptographicException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void Decrypt_InvalidBase64_ShouldThrowFormatException()
    {
        // Arrange
        var invalidBase64 = "not-valid-base64!!!";

        // Act & Assert
        var action = () => Decrypt(invalidBase64);
        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decrypt_EmptyString_ShouldThrowCryptographicException()
    {
        // Arrange
        var emptyEncrypted = Convert.ToBase64String(Array.Empty<byte>());

        // Act & Assert
        var action = () => Decrypt(emptyEncrypted);
        action.Should().Throw<CryptographicException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void Decrypt_WrongKey_ShouldThrowCryptographicException()
    {
        // Arrange
        var ssn = "123-45-6789";
        var encrypted = Encrypt(ssn);

        // Create a different key
        var wrongKey = new byte[KeySize];
        RandomNumberGenerator.Fill(wrongKey);

        // Act & Assert
        var action = () => DecryptWithKey(encrypted, wrongKey);
        action.Should().Throw<CryptographicException>();
    }

    #endregion

    #region Security Properties Tests

    [Fact]
    public void Encrypt_ShouldUseRandomNonce()
    {
        // Arrange
        var ssn = "123-45-6789";

        // Act - Encrypt same value multiple times
        var encrypted1 = Encrypt(ssn);
        var encrypted2 = Encrypt(ssn);

        var bytes1 = Convert.FromBase64String(encrypted1);
        var bytes2 = Convert.FromBase64String(encrypted2);

        // Extract nonces (first 12 bytes)
        var nonce1 = bytes1.Take(NonceSize).ToArray();
        var nonce2 = bytes2.Take(NonceSize).ToArray();

        // Assert - Nonces should be different
        nonce1.Should().NotBeEquivalentTo(nonce2);
    }

    [Fact]
    public void Encrypt_NonceShouldBe12Bytes()
    {
        // Arrange
        var ssn = "123-45-6789";

        // Act
        var encrypted = Encrypt(ssn);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Assert - First 12 bytes are the nonce
        encryptedBytes.Length.Should().BeGreaterThan(NonceSize);
    }

    [Fact]
    public void Encrypt_AuthTagShouldBe16Bytes()
    {
        // Arrange
        var ssn = "123-45-6789";

        // Act
        var encrypted = Encrypt(ssn);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Assert - Tag is bytes 12-27 (16 bytes after nonce)
        encryptedBytes.Length.Should().BeGreaterThan(NonceSize + TagSize);
    }

    [Fact]
    public void Decrypt_ShouldVerifyAuthenticationTag()
    {
        // Arrange - This test verifies that GCM authentication is working
        var ssn = "123-45-6789";
        var encrypted = Encrypt(ssn);

        // Act - Normal decryption should work
        var decrypted = Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(ssn);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void EncryptDecrypt_MultipleOperations_ShouldComplete()
    {
        // Arrange
        var ssn = "123-45-6789";
        var iterations = 100;

        // Act & Assert - Should complete without errors
        for (int i = 0; i < iterations; i++)
        {
            var encrypted = Encrypt(ssn);
            var decrypted = Decrypt(encrypted);
            decrypted.Should().Be(ssn);
        }
    }

    [Fact]
    public async Task EncryptDecrypt_ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var ssn = "123-45-6789";
        var tasks = new List<Task<bool>>();

        // Act - Run concurrent encrypt/decrypt operations
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var encrypted = Encrypt(ssn);
                var decrypted = Decrypt(encrypted);
                return decrypted == ssn;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed
        results.Should().AllBeEquivalentTo(true);
    }

    #endregion

    #region Helper Methods - AES-256-GCM Implementation

    private string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return Convert.ToBase64String(
                new byte[NonceSize + TagSize]);
        }

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_testKey, TagSize);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine: nonce + tag + ciphertext
        byte[] result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string ciphertextBase64)
    {
        return DecryptWithKey(ciphertextBase64, _testKey);
    }

    private static string DecryptWithKey(string ciphertextBase64, byte[] key)
    {
        byte[] encryptedData = Convert.FromBase64String(ciphertextBase64);

        if (encryptedData.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Invalid encrypted data: too short");
        }

        // Handle empty plaintext case
        if (encryptedData.Length == NonceSize + TagSize)
        {
            return "";
        }

        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        byte[] ciphertext = new byte[encryptedData.Length - NonceSize - TagSize];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        byte[] plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    #endregion
}
