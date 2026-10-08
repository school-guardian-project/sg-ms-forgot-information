using ms_forgot_information.Api.EmailChange.Application.Dto;

namespace ms_forgot_information.Api.EmailChange.Domain.Ports.In;

/// <summary>Step 1: sends a code to the CURRENT mailbox.</summary>
public interface IRequestEmailChangeUseCase
{
    Task ExecuteAsync(RequestEmailChangeDto dto, string requestIp, CancellationToken ct);
}

/// <summary>Steps 2 and 4: verifies the code sent to the current mailbox, or to the new one.</summary>
public interface IVerifyEmailChangeCodeUseCase
{
    Task<VerifyEmailChangeCodeResponseDto> VerifyCurrentAsync(VerifyEmailChangeCodeDto dto, CancellationToken ct);
    Task<VerifyEmailChangeCodeResponseDto> VerifyNewAsync(VerifyEmailChangeCodeDto dto, CancellationToken ct);
}

/// <summary>Step 3: with a verified current mailbox, sends a code to the NEW mailbox.</summary>
public interface ISubmitNewEmailUseCase
{
    Task ExecuteAsync(SubmitNewEmailDto dto, string requestIp, CancellationToken ct);
}

/// <summary>Step 5: applies the change once both mailboxes have been proven.</summary>
public interface IConfirmEmailChangeUseCase
{
    Task ExecuteAsync(ConfirmEmailChangeDto dto, CancellationToken ct);
}
/// <summary>Resends the code to the NEW mailbox after step 3.</summary>
public interface IResendNewEmailUseCase
{
    Task ExecuteAsync(ResendNewEmailDto dto, string requestIp, CancellationToken ct);
}
