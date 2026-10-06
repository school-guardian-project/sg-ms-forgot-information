namespace ms_forgot_information.Api.Shared.Domain.Model;

/// <summary>What the verification code authorizes once confirmed.</summary>
public enum Purpose
{
    PasswordReset,
    // Proves ownership of the current mailbox before an email change is allowed.
    EmailChange,
    // Proves ownership of the NEW mailbox; Target holds the new address.
    EmailChangeConfirm
}
