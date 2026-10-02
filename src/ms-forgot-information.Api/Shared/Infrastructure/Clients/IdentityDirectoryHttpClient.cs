using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.Shared.Infrastructure.Clients;

/// <summary>
/// REST bridge to iam-service and user-management-service. Temporary: per
/// sg-docs/.../12-forgot-information/decisions.md (DEC-002) this should become a gRPC client
/// once both services expose the documented .proto contracts — only this class would change.
/// </summary>
public class IdentityDirectoryHttpClient(
    IHttpClientFactory httpClientFactory,
    ILogger<IdentityDirectoryHttpClient> logger) : IIdentityDirectoryClient
{
    private const string IamClientName = "iam-service";
    private const string UserManagementClientName = "user-management-service";

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

    public async Task<ContactInfo> GetContactInfoAsync(Guid profileId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(UserManagementClientName);
        var response = await client.GetAsync($"/api/persons/by-profile/{profileId}/contact", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ContactInfo>(ct)
            ?? throw new InvalidOperationException("Empty contact info response");
    }

    public async Task<bool> EmailInUseAsync(string email, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(UserManagementClientName);
        var response = await client.GetAsync($"/api/persons/email-exists?email={Uri.EscapeDataString(email)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(ct);
    }

    public async Task<bool> PhoneInUseAsync(string phone, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(UserManagementClientName);
        var response = await client.GetAsync($"/api/persons/phone-exists?phone={Uri.EscapeDataString(phone)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(ct);
    }

    public async Task<bool> ValidateCurrentPasswordAsync(Guid profileId, string currentPassword, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(IamClientName);
        var response = await client.PostAsJsonAsync(
            $"/api/profiles/{profileId}/validate-password", new { password = currentPassword }, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(ct);
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
        var client = httpClientFactory.CreateClient(UserManagementClientName);
        var response = await client.PutAsJsonAsync(
            $"/api/persons/by-profile/{profileId}/email", new { email = newEmail }, ct);

        LogAndEnsureSuccess(response, "update email", profileId);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdatePhoneAsync(Guid profileId, string newPhone, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(UserManagementClientName);
        var response = await client.PutAsJsonAsync(
            $"/api/persons/by-profile/{profileId}/phone", new { phone = newPhone }, ct);

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
