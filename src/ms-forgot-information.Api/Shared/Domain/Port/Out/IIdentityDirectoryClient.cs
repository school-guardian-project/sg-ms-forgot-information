namespace ms_forgot_information.Api.Shared.Domain.Port.Out;

/// <summary>
/// Synchronous bridge to the services that own the actual account data. This service never
/// reads/writes Person or Profile/password-hash data directly (see ADR-002/ADR-005 and
/// sg-docs/09-microservices/services/12-forgot-information/decisions.md, DEC-002).
/// REST/HTTP today; swap the implementation for gRPC once iam-service/user-management-service
/// expose the .proto contracts described in that decision — the interface does not change.
/// </summary>
public interface IIdentityDirectoryClient
{
    /// <summary>Resolves a profile by its login email. Returns null without throwing if not found.</summary>
    Task<ProfileLookup?> FindProfileByEmailAsync(string email, CancellationToken ct);

    Task<ContactInfo> GetContactInfoAsync(Guid profileId, CancellationToken ct);

    Task<bool> EmailInUseAsync(string email, CancellationToken ct);

    Task<bool> PhoneInUseAsync(string phone, CancellationToken ct);

    Task<bool> ValidateCurrentPasswordAsync(Guid profileId, string currentPassword, CancellationToken ct);

    /// <summary>iam-service hashes the password itself; this service only forwards the new plaintext over the internal network.</summary>
    Task UpdatePasswordAsync(Guid profileId, string newPassword, CancellationToken ct);

    Task UpdateEmailAsync(Guid profileId, string newEmail, CancellationToken ct);

    Task UpdatePhoneAsync(Guid profileId, string newPhone, CancellationToken ct);
}

public sealed record ProfileLookup(Guid ProfileId, string Email);

public sealed record ContactInfo(string Email, string? Phone);
