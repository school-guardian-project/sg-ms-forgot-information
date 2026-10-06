using ms_forgot_information.Api.PhoneChange.Application.Dto;

namespace ms_forgot_information.Api.PhoneChange.Domain.Ports.In;

/// <summary>Step 1: emails a code to the CURRENT mailbox to prove who is asking.</summary>
public interface IRequestPhoneChangeUseCase
{
    Task ExecuteAsync(RequestPhoneChangeDto dto, string requestIp, CancellationToken ct);
}

/// <summary>Step 2: checks the email code.</summary>
public interface IVerifyPhoneChangeIdentityUseCase
{
    Task<VerifyPhoneChangeIdentityResponseDto> ExecuteAsync(VerifyPhoneChangeIdentityDto dto, CancellationToken ct);
}

/// <summary>Step 3: asks Twilio Verify to send an SMS to the NEW phone.</summary>
public interface IRequestPhoneVerificationUseCase
{
    Task ExecuteAsync(RequestPhoneVerificationDto dto, string requestIp, CancellationToken ct);
}

/// <summary>Step 4: asks Twilio Verify to check the SMS code and, only if approved, updates the phone.</summary>
public interface ICheckPhoneVerificationUseCase
{
    Task ExecuteAsync(CheckPhoneVerificationDto dto, CancellationToken ct);
}