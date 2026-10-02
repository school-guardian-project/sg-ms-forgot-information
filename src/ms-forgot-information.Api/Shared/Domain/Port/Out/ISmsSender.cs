namespace ms_forgot_information.Api.Shared.Domain.Port.Out;

public interface ISmsSender
{
    /// <summary>`toPhoneE164` must be in E.164 format (e.g. +573001234567).</summary>
    Task SendAsync(string toPhoneE164, string message, CancellationToken ct);
}
