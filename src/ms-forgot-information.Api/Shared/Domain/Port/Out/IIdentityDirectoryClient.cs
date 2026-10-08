namespace ms_forgot_information.Api.Shared.Domain.Port.Out;

/// <summary>
/// Bridge to iam-service, which owns user profiles and password hashes.
/// This service never reads or writes password data directly.
/// </summary>
public interface IIdentityDirectoryClient
{
    /// <summary>Resolves a profile by its login email. Returns null without throwing if not found.</summary>
    Task<ProfileLookup?> FindProfileByEmailAsync(string email, CancellationToken ct);

    /// <summary>True when the phone currently stored for the profile equals the given E.164 number (the stored number is never returned).</summary>
    Task<bool> PhoneMatchesAsync(Guid profileId, string phoneE164, CancellationToken ct);

    /// <summary>iam-service hashes the password itself; this service only forwards the new plaintext over the internal network.</summary>
    Task UpdatePasswordAsync(Guid profileId, string newPassword, CancellationToken ct);

    /// <summary>Replaces the login email. Throws <c>EmailAlreadyInUseException</c> when IAM reports a conflict.</summary>
    /// <summary>Replaces the profile phone. Accepts E.164; IAM normalizes it to the stored format.</summary>
    Task UpdatePhoneAsync(Guid profileId, string newPhoneE164, CancellationToken ct);

    Task UpdateEmailAsync(Guid profileId, string newEmail, CancellationToken ct);
}

public sealed record ProfileLookup(Guid ProfileId, string Email);
