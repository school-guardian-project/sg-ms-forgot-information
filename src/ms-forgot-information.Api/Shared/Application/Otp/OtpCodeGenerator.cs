using System.Security.Cryptography;

namespace ms_forgot_information.Api.Shared.Application.Otp;

/// <summary>Generates numeric OTP codes and opaque reset tokens using a CSPRNG (never <see cref="Random"/>).</summary>
public static class OtpCodeGenerator
{
    public static string GenerateNumericCode(int length)
    {
        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);

        var digits = new char[length];
        for (var i = 0; i < length; i++)
        {
            digits[i] = (char)('0' + buffer[i] % 10);
        }

        return new string(digits);
    }

    public static string GenerateOpaqueToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
