namespace ms_forgot_information.Api.Shared.Domain.Model;

/// <summary>What the verification code authorizes once confirmed.</summary>
public enum Purpose
{
    PasswordReset,
    ChangePassword,
    ChangeEmail,
    ChangePhone
}
