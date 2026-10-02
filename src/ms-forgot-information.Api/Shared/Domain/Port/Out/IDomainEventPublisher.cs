namespace ms_forgot_information.Api.Shared.Domain.Port.Out;

/// <summary>
/// Seam for the Kafka events documented in
/// sg-docs/09-microservices/services/12-forgot-information/events.md. No Kafka broker is
/// deployed anywhere in the project yet (see ADR-004 status), so the registered implementation
/// only logs; swap it for a real producer once the broker exists, without touching callers.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(string eventType, string topic, object payload, CancellationToken ct);
}
