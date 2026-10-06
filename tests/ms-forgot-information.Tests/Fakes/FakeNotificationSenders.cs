using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Tests.Fakes;

public class FakeEmailSender : IEmailSender
{
    public readonly List<(string To, string Subject, string Body)> Sent = new();

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        Sent.Add((toEmail, subject, body));
        return Task.CompletedTask;
    }
}
