using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Tests.Fakes;

public class FakeSmsVerificationService : ISmsVerificationService
{
    public readonly List<string> StartedFor = new();
    public readonly List<(string Phone, string Code)> Checked = new();
    public SmsCheckResult NextResult = SmsCheckResult.Approved;
    public Exception? ThrowOnStart;

    public Task StartAsync(string e164Phone, CancellationToken ct)
    {
        if (ThrowOnStart is not null) throw ThrowOnStart;
        StartedFor.Add(e164Phone);
        return Task.CompletedTask;
    }

    public Task<SmsCheckResult> CheckAsync(string e164Phone, string code, CancellationToken ct)
    {
        Checked.Add((e164Phone, code));
        return Task.FromResult(NextResult);
    }
}