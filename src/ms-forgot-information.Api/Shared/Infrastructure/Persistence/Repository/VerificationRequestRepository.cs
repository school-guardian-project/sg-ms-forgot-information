using Microsoft.EntityFrameworkCore;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Context;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Entity;

namespace ms_forgot_information.Api.Shared.Infrastructure.Persistence.Repository;

public class VerificationRequestRepository(ForgotInformationContext context) : IVerificationRequestRepository
{
    public async Task AddAsync(VerificationRequest request, CancellationToken ct)
    {
        context.VerificationRequests.Add(ToEntity(request));
        await context.SaveChangesAsync(ct);
    }

    public async Task<VerificationRequest?> GetActiveAsync(Guid profileId, Purpose purpose, CancellationToken ct)
    {
        var entity = await context.VerificationRequests
            .Where(x => x.ProfileId == profileId && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<VerificationRequest?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await context.VerificationRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpdateAsync(VerificationRequest request, CancellationToken ct)
    {
        var entity = await context.VerificationRequests.FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new InvalidOperationException($"VerificationRequest {request.Id} not found");

        CopyMutableState(request, entity);
        await context.SaveChangesAsync(ct);
    }

    public Task<int> CountRecentAsync(Guid profileId, Purpose purpose, DateTime since, CancellationToken ct)
    {
        return context.VerificationRequests
            .Where(x => x.ProfileId == profileId && x.Purpose == purpose && x.CreatedAt >= since)
            .CountAsync(ct);
    }

    private static VerificationRequestEntity ToEntity(VerificationRequest request) => new()
    {
        Id = request.Id,
        ProfileId = request.ProfileId,
        Purpose = request.Purpose,
        Target = request.Target,
        CodeHash = request.CodeHash,
        ResetTokenHash = request.ResetTokenHash,
        ExpiresAt = request.ExpiresAt,
        AttemptCount = request.AttemptCount,
        MaxAttempts = request.MaxAttempts,
        Status = request.Status,
        CreatedAt = request.CreatedAt,
        VerifiedAt = request.VerifiedAt,
        ConsumedAt = request.ConsumedAt,
        RequestIp = request.RequestIp
    };

    private static void CopyMutableState(VerificationRequest request, VerificationRequestEntity entity)
    {
        entity.ResetTokenHash = request.ResetTokenHash;
        entity.AttemptCount = request.AttemptCount;
        entity.Status = request.Status;
        entity.VerifiedAt = request.VerifiedAt;
        entity.ConsumedAt = request.ConsumedAt;
    }

    private static VerificationRequest ToDomain(VerificationRequestEntity entity)
    {
        return VerificationRequest.Rehydrate(
            entity.Id, entity.ProfileId, entity.Purpose, entity.Target, entity.CodeHash,
            entity.ResetTokenHash, entity.ExpiresAt, entity.AttemptCount, entity.MaxAttempts,
            entity.Status, entity.CreatedAt, entity.VerifiedAt, entity.ConsumedAt, entity.RequestIp);
    }
}
