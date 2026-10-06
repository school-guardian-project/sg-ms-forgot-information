using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.Shared.Infrastructure.Clients;

/// <summary>
/// REST bridge to the IAM service, which owns profile lookup and password updates.
/// </summary>
public class IdentityDirectoryHttpClient(
    IHttpClientFactory httpClientFactory,
    ILogger<IdentityDirectoryHttpClient> logger) : IIdentityDirectoryClient
{
    private const string IamClientName = "iam-service";

    public async Task<ProfileLookup?> FindProfileByEmailAsync(string email, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(IamClientName);
        var response = await client.GetAsync($"/api/profiles/by-email?email={Uri.EscapeDataString(email)}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileLookup>(ct);
    }

    private sealed record PhoneMatchResponse(bool Matches);

    public async Task<bool> PhoneMatchesAsync(Guid profileId, string phoneE164, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(IamClientName);
        var response = await client.PostAsJsonAsync(
            $"/api/profiles/{profileId}/phone/matches", new { phone = phoneE164 }, ct);

        if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
        {
            return false;
        }

        LogAndEnsureSuccess(response, "match phone", profileId);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PhoneMatchResponse>(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web), ct);
        return body?.Matches == true;
    }

    public async Task UpdatePasswordAsync(Guid profileId, string newPassword, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(IamClientName);
        var response = await client.PutAsJsonAsync(
            $"/api/profiles/{profileId}/password", new { password = newPassword }, ct);

        LogAndEnsureSuccess(response, "update password", profileId);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateEmailAsync(Guid profileId, string newEmail, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(IamClientName);
        var response = await client.PutAsJsonAsync(
            $"/api/profiles/{profileId}/email", new { email = newEmail }, ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new EmailAlreadyInUseException();
        }

        LogAndEnsureSuccess(response, "update email", profileId);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdatePhoneAsync(Guid profileId, string newPhoneE164, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(IamClientName);
        var response = await client.PutAsJsonAsync(
            $"/api/profiles/{profileId}/phone", new { phone = newPhoneE164 }, ct);

        LogAndEnsureSuccess(response, "update phone", profileId);
        response.EnsureSuccessStatusCode();
    }

    private void LogAndEnsureSuccess(HttpResponseMessage response, string operation, Guid profileId)
    {
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Upstream {Operation} failed for profile {ProfileId} with status {Status}",
                operation, profileId, response.StatusCode);
        }
    }
}
