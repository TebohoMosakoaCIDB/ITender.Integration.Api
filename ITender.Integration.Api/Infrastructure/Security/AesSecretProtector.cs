using System.Security.Cryptography;
using System.Text;
using ITender.Integration.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ITender.Integration.Api.Infrastructure.Security;

public interface ISecretProtector
{
    string Unprotect(string value);
}

public sealed class AesSecretProtector : ISecretProtector
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesSecretProtector(IOptions<EncryptionOptions> options)
    {
        var encryptionOptions = options.Value;

        if (string.IsNullOrWhiteSpace(encryptionOptions.Key) ||
            encryptionOptions.Key.Length < 11)
        {
            throw new InvalidOperationException(
                "Encryption:Key must contain at least 11 characters " +
                "for the existing DES key derivation.");
        }

        if (string.IsNullOrWhiteSpace(encryptionOptions.IV) ||
            encryptionOptions.IV.Length < 13)
        {
            throw new InvalidOperationException(
                "Encryption:IV must contain at least 13 characters " +
                "for the existing DES IV derivation.");
        }

        // Preserve the existing iTender.Compliance derivation.
        _key = Encoding.UTF8.GetBytes(
            encryptionOptions.Key.Substring(3, 8));

        _iv = Encoding.UTF8.GetBytes(
            encryptionOptions.IV.Substring(5, 8));
    }

    public string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        byte[] cipherBytes;

        try
        {
            cipherBytes = Convert.FromBase64String(value.Replace(" ", "+"));
        }
        catch (FormatException)
        {
            // Legacy CRM values may already be plain text.
            return value;
        }

        // DES uses an 8-byte block size. Reject decoded values that cannot
        // represent complete DES ciphertext blocks.
        if (cipherBytes.Length == 0 || cipherBytes.Length % 8 != 0)
            return value;

        try
        {
            using var des = DES.Create();
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;

            using var decryptor = des.CreateDecryptor(_key, _iv);

            var plaintext = decryptor.TransformFinalBlock(
                cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "The stored value appears to be encrypted, but decryption " +
                "failed. Verify the legacy DES key, IV, and encryption settings.",
                ex);
        }
    }
}
