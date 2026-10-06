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

    // Mirrors the policy already enforced client-side in the Angular reset-password form.
    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.\-_])[A-Za-z\d@$!%*?&.\-_]{8,}$")]
    private static partial Regex PasswordPattern();
}
