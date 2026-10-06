using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ms_forgot_information.Api.Shared.Domain.Exceptions;

namespace ms_forgot_information.Api.Shared.Application.Validation;

public static partial class InputValidators
{
    public static void ValidateNewPassword(string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            throw new PasswordMismatchException();
        }

        if (!PasswordPattern().IsMatch(newPassword))
        {
            throw new WeakPasswordException();
        }
    }

    public static string NormalizeEmail(string email)
    {
        var normalized = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || !new EmailAddressAttribute().IsValid(normalized))
        {
            throw new InvalidContactFormatException("El correo electrónico no tiene un formato válido.");
        }

        return normalized.ToLowerInvariant();
    }

    /// <summary>Requires E.164 (+, country code, 8-15 digits). No country is ever assumed.</summary>
    public static string NormalizePhone(string phone)
    {
        var normalized = phone?.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
        if (string.IsNullOrWhiteSpace(normalized) || !E164Pattern().IsMatch(normalized))
        {
            throw new InvalidContactFormatException("El teléfono debe estar en formato internacional, por ejemplo +573001234567.");
        }

        return normalized;
    }

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164Pattern();

    // Mirrors the policy already enforced client-side in the Angular reset-password form.
    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.\-_])[A-Za-z\d@$!%*?&.\-_]{8,}$")]
    private static partial Regex PasswordPattern();
}
