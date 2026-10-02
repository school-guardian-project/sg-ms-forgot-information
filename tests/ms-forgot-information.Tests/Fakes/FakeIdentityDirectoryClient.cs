using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Tests.Fakes;

public class FakeIdentityDirectoryClient : IIdentityDirectoryClient
{
    public readonly Dictionary<string, Guid> ProfilesByEmail = new();
    public readonly HashSet<string> EmailsInUse = new();
    public readonly HashSet<string> PhonesInUse = new();
    public bool CurrentPasswordValid = true;
    public string? LastPasswordUpdate;
    public string? LastEmailUpdate;
    public string? LastPhoneUpdate;
    public bool ThrowOnNextUpdate;

    public Task<ProfileLookup?> FindProfileByEmailAsync(string email, CancellationToken ct)
    {
        return Task.FromResult(ProfilesByEmail.TryGetValue(email, out var id)
            ? new ProfileLookup(id, email)
            : null);
    }

    public Task<ContactInfo> GetContactInfoAsync(Guid profileId, CancellationToken ct) =>
        Task.FromResult(new ContactInfo("user@example.com", "+573000000000"));

    public Task<bool> EmailInUseAsync(string email, CancellationToken ct) => Task.FromResult(EmailsInUse.Contains(email));

    public Task<bool> PhoneInUseAsync(string phone, CancellationToken ct) => Task.FromResult(PhonesInUse.Contains(phone));

    public Task<bool> ValidateCurrentPasswordAsync(Guid profileId, string currentPassword, CancellationToken ct) =>
        Task.FromResult(CurrentPasswordValid);

    public Task UpdatePasswordAsync(Guid profileId, string newPassword, CancellationToken ct)
    {
        if (ThrowOnNextUpdate) throw new InvalidOperationException("simulated upstream failure");
        LastPasswordUpdate = newPassword;
        return Task.CompletedTask;
    }

    public Task UpdateEmailAsync(Guid profileId, string newEmail, CancellationToken ct)
    {
        if (ThrowOnNextUpdate) throw new InvalidOperationException("simulated upstream failure");
        LastEmailUpdate = newEmail;
        return Task.CompletedTask;
    }

    public Task UpdatePhoneAsync(Guid profileId, string newPhone, CancellationToken ct)
    {
        if (ThrowOnNextUpdate) throw new InvalidOperationException("simulated upstream failure");
        LastPhoneUpdate = newPhone;
        return Task.CompletedTask;
    }
}
