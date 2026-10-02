using ms_forgot_information.Api.Shared.Domain.Model;

namespace ms_forgot_information.Api.Shared.Domain.Port.Out;

public interface IVerificationRequestRepository
{
    Task AddAsync(VerificationRequest request, CancellationToken ct);

    /// <summary>
    /// The most recent request for this profile+purpose, regardless of status — callers decide
    /// how to react to Locked/Expired/Consumed (see VerificationCodeService). Only the most
    /// recent one matters since issuing a new code supersedes any previous one.
    /// </summary>
    Task<VerificationRequest?> GetActiveAsync(Guid profileId, Purpose purpose, CancellationToken ct);

    Task<VerificationRequest?> GetByIdAsync(Guid id, CancellationToken ct);

    Task UpdateAsync(VerificationRequest request, CancellationToken ct);

    /// <summary>Count of requests issued for this profile+purpose since <paramref name="since"/>, for rate limiting.</summary>
    Task<int> CountRecentAsync(Guid profileId, Purpose purpose, DateTime since, CancellationToken ct);
}
