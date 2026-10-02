using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Tests.Fakes;

public class FakeVerificationRequestRepository : IVerificationRequestRepository
{
    private readonly Dictionary<Guid, VerificationRequest> _store = new();

    public Task AddAsync(VerificationRequest request, CancellationToken ct)
    {
        _store[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<VerificationRequest?> GetActiveAsync(Guid profileId, Purpose purpose, CancellationToken ct)
    {
        var match = _store.Values
            .Where(x => x.ProfileId == profileId && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(match);
    }

    public Task<VerificationRequest?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var request);
        return Task.FromResult(request);
    }

    public Task UpdateAsync(VerificationRequest request, CancellationToken ct)
    {
        _store[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<int> CountRecentAsync(Guid profileId, Purpose purpose, DateTime since, CancellationToken ct)
    {
        var count = _store.Values.Count(x => x.ProfileId == profileId && x.Purpose == purpose && x.CreatedAt >= since);
        return Task.FromResult(count);
    }

    public int Count => _store.Count;
}
