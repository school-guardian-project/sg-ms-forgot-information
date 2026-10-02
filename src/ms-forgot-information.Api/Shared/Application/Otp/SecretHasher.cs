using System.Security.Cryptography;
using System.Text;

namespace ms_forgot_information.Api.Shared.Application.Otp;

public interface ISecretHasher
{
    /// <summary>Returns "{saltBase64}.{hashBase64}" — never the plaintext value, never a reversible format.</summary>
    string Hash(string plainValue);

    bool Verify(string plainValue, string storedHash);
}

/// <summary>HMAC-SHA256 with a per-value random salt plus a server-side pepper from configuration.</summary>
public sealed class SecretHasher(string pepper) : ISecretHasher
{
    public string Hash(string plainValue)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = ComputeHash(plainValue, salt);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string plainValue, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = ComputeHash(plainValue, salt);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private byte[] ComputeHash(string plainValue, byte[] salt)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var input = new byte[salt.Length + Encoding.UTF8.GetByteCount(plainValue)];
        salt.CopyTo(input, 0);
        Encoding.UTF8.GetBytes(plainValue, 0, plainValue.Length, input, salt.Length);
        return hmac.ComputeHash(input);
    }
}
