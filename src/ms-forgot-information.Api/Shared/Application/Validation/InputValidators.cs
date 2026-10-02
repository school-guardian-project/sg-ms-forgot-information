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

    public static void ValidateEmail(string email)
    {
        if (!EmailPattern().IsMatch(email))
        {
            throw new InvalidContactFormatException("El correo electrónico no tiene un formato válido.");
        }
    }

    public static void ValidatePhone(string phone)
    {
        if (!PhonePattern().IsMatch(phone))
        {
            throw new InvalidContactFormatException("El número de teléfono no tiene un formato válido (usa formato E.164, ej. +573001234567).");
        }
    }

    // Mirrors the policy already enforced client-side in the Angular reset-password form.
    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.\-_])[A-Za-z\d@$!%*?&.\-_]{8,}$")]
    private static partial Regex PasswordPattern();

    [GeneratedRegex(@"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,4}$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+?[1-9]\d{1,14}$")]
    private static partial Regex PhonePattern();
}
