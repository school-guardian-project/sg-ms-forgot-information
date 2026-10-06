using System.ComponentModel.DataAnnotations;

namespace ms_forgot_information.Api.PhoneChange.Application.Dto;

/// <summary>
/// Identity step: Email identifies the profile (taken from the session) and CurrentPhone is the E.164 number
/// the user claims to own. The SMS is sent only if it matches the phone stored for that profile.
/// </summary>
public sealed record RequestPhoneChangeDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required, MaxLength(20)] string CurrentPhone);

public sealed record VerifyPhoneChangeIdentityDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required, MaxLength(20)] string CurrentPhone,
    [param: Required, RegularExpression(@"^\d{4,10}$")] string Code);

public sealed record VerifyPhoneChangeIdentityResponseDto(string ResetToken);

/// <summary>NewPhone is the E.164 number that will receive the SMS, passed to Twilio exactly as validated.</summary>
public sealed record RequestPhoneVerificationDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required] string ResetToken,
    [param: Required, MaxLength(20)] string NewPhone);

public sealed record CheckPhoneVerificationDto(
    [param: Required, EmailAddress, MaxLength(100)] string Email,
    [param: Required, MaxLength(20)] string NewPhone,
    [param: Required, RegularExpression(@"^\d{4,10}$")] string Code);