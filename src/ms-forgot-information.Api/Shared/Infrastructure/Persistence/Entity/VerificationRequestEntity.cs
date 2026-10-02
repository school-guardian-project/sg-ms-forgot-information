using ms_forgot_information.Api.Shared.Domain.Model;

namespace ms_forgot_information.Api.Shared.Infrastructure.Persistence.Entity;

public class VerificationRequestEntity
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public Purpose Purpose { get; set; }
    public string Target { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public string? ResetTokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public byte AttemptCount { get; set; }
    public byte MaxAttempts { get; set; }
    public VerificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string RequestIp { get; set; } = null!;
}
