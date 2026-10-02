namespace ms_forgot_information.Api.Password.Application.Dto;

public sealed record ForgotPasswordRequestDto(string Email);

public sealed record VerifyPasswordResetCodeRequestDto(string Email, string Code);

public sealed record VerifyPasswordResetCodeResponseDto(string ResetToken);

public sealed record ResetPasswordRequestDto(string Email, string ResetToken, string NewPassword, string ConfirmPassword);

public sealed record ChangePasswordRequestDto(string? CurrentPassword, string? Code, string NewPassword, string ConfirmPassword);
