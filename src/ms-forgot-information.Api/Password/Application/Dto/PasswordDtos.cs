using System.ComponentModel.DataAnnotations;

namespace ms_forgot_information.Api.Password.Application.Dto;

public sealed record ForgotPasswordRequestDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email);

public sealed record VerifyPasswordResetCodeRequestDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required, RegularExpression(@"^\d{6}$")] string Code);

public sealed record VerifyPasswordResetCodeResponseDto(string ResetToken);

public sealed record ResetPasswordRequestDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required] string ResetToken,
    [param: Required, MaxLength(128)] string NewPassword,
    [param: Required, MaxLength(128)] string ConfirmPassword);
