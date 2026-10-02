using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.Shared.Infrastructure.Events;

/// <summary>Default publisher until a real Kafka broker is deployed (see IDomainEventPublisher).</summary>
public class LoggingDomainEventPublisher(ILogger<LoggingDomainEventPublisher> logger) : IDomainEventPublisher
{
    public Task PublishAsync(string eventType, string topic, object payload, CancellationToken ct)
    {
        logger.LogInformation("[event] {EventType} -> {Topic}: {@Payload}", eventType, topic, payload);
        return Task.CompletedTask;
    }
}
