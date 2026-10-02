using ms_forgot_information.Api.Shared.Domain.Exceptions;

namespace ms_forgot_information.Api.Shared.Domain.Model;

/// <summary>
/// A single OTP challenge. Owns its own lifecycle: a code can only move
/// Pending/Verified -> Consumed once, and only forward in time.
/// </summary>
public class VerificationRequest
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public Purpose Purpose { get; private set; }
    public string Target { get; private set; } = null!;
    public string CodeHash { get; private set; } = null!;
    public string? ResetTokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public byte AttemptCount { get; private set; }
    public byte MaxAttempts { get; private set; }
    public VerificationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public string RequestIp { get; private set; } = null!;

    private VerificationRequest() { }

    public static VerificationRequest Issue(
        Guid profileId,
        Purpose purpose,
        string target,
        string codeHash,
        TimeSpan expiration,
        byte maxAttempts,
        string requestIp,
        DateTime utcNow)
    {
        return new VerificationRequest
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Purpose = purpose,
            Target = target,
            CodeHash = codeHash,
            ExpiresAt = utcNow.Add(expiration),
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            Status = VerificationStatus.Pending,
            CreatedAt = utcNow,
            RequestIp = requestIp
        };
    }

    /// <summary>Reconstructs an instance from persisted state. Never use this to create a new request.</summary>
    public static VerificationRequest Rehydrate(
        Guid id, Guid profileId, Purpose purpose, string target, string codeHash,
        string? resetTokenHash, DateTime expiresAt, byte attemptCount, byte maxAttempts,
        VerificationStatus status, DateTime createdAt, DateTime? verifiedAt, DateTime? consumedAt, string requestIp)
    {
        return new VerificationRequest
        {
            Id = id,
            ProfileId = profileId,
            Purpose = purpose,
            Target = target,
            CodeHash = codeHash,
            ResetTokenHash = resetTokenHash,
            ExpiresAt = expiresAt,
            AttemptCount = attemptCount,
            MaxAttempts = maxAttempts,
            Status = status,
            CreatedAt = createdAt,
            VerifiedAt = verifiedAt,
            ConsumedAt = consumedAt,
            RequestIp = requestIp
        };
    }

    public bool CanBeChallenged(DateTime utcNow) =>
        Status is VerificationStatus.Pending or VerificationStatus.Verified && utcNow < ExpiresAt;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public void MarkExpired() => Status = VerificationStatus.Expired;

    public void MarkLocked() => Status = VerificationStatus.Locked;

    /// <summary>Registers a failed comparison; locks the request once attempts are exhausted.</summary>
    public void RegisterFailedAttempt()
    {
        AttemptCount++;
        if (AttemptCount >= MaxAttempts)
        {
            Status = VerificationStatus.Locked;
        }
    }

    /// <summary>Marks the code as matched and issues a short-lived opaque token for the final step.</summary>
    public void MarkVerified(string resetTokenHash, DateTime utcNow)
    {
        if (!CanBeChallenged(utcNow))
        {
            throw new InvalidCodeException();
        }

        ResetTokenHash = resetTokenHash;
        VerifiedAt ??= utcNow;
        Status = VerificationStatus.Verified;
    }

    /// <summary>Burns the request only after the downstream update has actually succeeded.</summary>
    public void MarkConsumed(DateTime utcNow)
    {
        Status = VerificationStatus.Consumed;
        ConsumedAt = utcNow;
    }
}
