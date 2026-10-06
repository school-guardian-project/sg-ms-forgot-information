using System.Security.Cryptography;
using System.Text;

namespace ms_forgot_information.Api.Shared.Application.Validation;

/// <summary>
/// Log-safe representation of an email: "j***@gmail.com#a1b2c3d4". The short hash lets you confirm that the
/// same address travels through every stage (controller → use case → SMTP) without writing the full address.
/// </summary>
public static class EmailLogMask
{
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "<empty>";
        }

        var at = email.IndexOf('@');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())))[..8];
        var masked = at > 0 ? $"{email[0]}***{email[at..]}" : "***";
        return $"{masked}#{hash}";
    }
}
