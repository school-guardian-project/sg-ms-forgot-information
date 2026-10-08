using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Tests.Fakes;

public class FakeIdentityDirectoryClient : IIdentityDirectoryClient
{
    public readonly Dictionary<string, Guid> ProfilesByEmail = new();
    public string? LastPasswordUpdate;
    public bool ThrowOnNextUpdate;

    public Task<ProfileLookup?> FindProfileByEmailAsync(string email, CancellationToken ct)
    {
        return Task.FromResult(ProfilesByEmail.TryGetValue(email, out var id)
            ? new ProfileLookup(id, email)
            : null);
    }

    public readonly Dictionary<Guid, string> StoredPhones = new();

    public Task<bool> PhoneMatchesAsync(Guid profileId, string phoneE164, CancellationToken ct) =>
        Task.FromResult(StoredPhones.TryGetValue(profileId, out var stored) && stored == phoneE164);

    public (Guid ProfileId, string Phone)? LastPhoneUpdate;

    public Task UpdatePhoneAsync(Guid profileId, string newPhoneE164, CancellationToken ct)
    {
        if (ThrowOnNextUpdate) throw new InvalidOperationException("simulated upstream failure");
        LastPhoneUpdate = (profileId, newPhoneE164);
        return Task.CompletedTask;
    }

    public (Guid ProfileId, string Email)? LastEmailUpdate;

    public Task UpdateEmailAsync(Guid profileId, string newEmail, CancellationToken ct)
    {
        if (ThrowOnNextUpdate) throw new InvalidOperationException("simulated upstream failure");
        LastEmailUpdate = (profileId, newEmail);
        return Task.CompletedTask;
    }

    public Task UpdatePasswordAsync(Guid profileId, string newPassword, CancellationToken ct)
    {
        if (ThrowOnNextUpdate) throw new InvalidOperationException("simulated upstream failure");
        LastPasswordUpdate = newPassword;
        return Task.CompletedTask;
    }
}
