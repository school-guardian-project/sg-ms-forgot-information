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

public class FakeSmsSender : ISmsSender
{
    public readonly List<(string To, string Message)> Sent = new();

    public Task SendAsync(string toPhoneE164, string message, CancellationToken ct)
    {
        Sent.Add((toPhoneE164, message));
        return Task.CompletedTask;
    }
}

public class FakeDomainEventPublisher : IDomainEventPublisher
{
    public readonly List<(string EventType, string Topic, object Payload)> Published = new();

    public Task PublishAsync(string eventType, string topic, object payload, CancellationToken ct)
    {
        Published.Add((eventType, topic, payload));
        return Task.CompletedTask;
    }
}
