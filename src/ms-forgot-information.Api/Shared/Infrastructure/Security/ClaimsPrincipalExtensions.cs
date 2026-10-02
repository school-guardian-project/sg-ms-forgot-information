using System.Security.Claims;
using ms_forgot_information.Api.Shared.Domain.Exceptions;

namespace ms_forgot_information.Api.Shared.Infrastructure.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the profile id from the JWT `sub` claim ONLY — callers must never trust a
    /// profileId supplied by the client, otherwise a user could modify another user's account.
    /// </summary>
    public static Guid GetProfileId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var profileId))
        {
            throw new InvalidCredentialsException();
        }

        return profileId;
    }
}
