using System.ComponentModel.DataAnnotations;

namespace ms_forgot_information.Api.EmailChange.Application.Dto;

/// <summary>Email is always the CURRENT login email of the profile that wants to change it.</summary>
public sealed record RequestEmailChangeDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email);

public sealed record VerifyEmailChangeCodeDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required, RegularExpression(@"^\d{6}$")] string Code);

public sealed record VerifyEmailChangeCodeResponseDto(string ResetToken);

public sealed record SubmitNewEmailDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required] string ResetToken,
    [param: Required, EmailAddress, MaxLength(100)] string NewEmail);

public sealed record ConfirmEmailChangeDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required] string ResetToken);