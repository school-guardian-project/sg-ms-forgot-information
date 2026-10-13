using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using DomainProfileLookup = ms_forgot_information.Api.Shared.Domain.Port.Out.ProfileLookup;

namespace ms_forgot_information.Api.Shared.Infrastructure.Grpc;

/// <summary>
/// gRPC bridge to the IAM service (contract iam.v1.InternalProfileService),
/// which owns profile lookup and password updates. Inter-service traffic is
/// gRPC-only (ADR-005); the shared secret travels in the x-internal-api-key
/// metadata header and comes from configuration, never from code.
/// </summary>
public sealed class IdentityDirectoryGrpcClient : IIdentityDirectoryClient, IDisposable
{
    // Holgura amplia: la primera llamada tras un arranque en frio puede tardar
    // varios segundos en el servicio destino.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private const string ApiKeyHeader = "x-internal-api-key";

    private readonly InternalProfileService.InternalProfileServiceClient _client;
    private readonly string _apiKey;
    private readonly ILogger<IdentityDirectoryGrpcClient> _logger;
    private readonly GrpcChannel? _channel;

    public IdentityDirectoryGrpcClient(
        IdentityDirectoryOptions options,
        ILogger<IdentityDirectoryGrpcClient> logger)
    {
        if (string.IsNullOrWhiteSpace(options.GrpcAddress))
        {
            throw new InvalidOperationException(
                "IdentityDirectory:GrpcAddress must be configured (e.g. http://ms-iam:5001).");
        }
        _channel = GrpcChannel.ForAddress(options.GrpcAddress);
        _client = new InternalProfileService.InternalProfileServiceClient(_channel);
        _apiKey = options.IamApiKey;
        _logger = logger;
    }

    public async Task<DomainProfileLookup?> FindProfileByEmailAsync(string email, CancellationToken ct)
    {
        try
        {
            var response = await _client.FindProfileByEmailAsync(
                new FindProfileByEmailRequest { Email = email },
                BuildHeaders(), Deadline(), ct);
            return new DomainProfileLookup(Guid.Parse(response.ProfileId), response.Email);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> PhoneMatchesAsync(Guid profileId, string phoneE164, CancellationToken ct)
    {
        try
        {
            var response = await _client.PhoneMatchesAsync(
                new PhoneMatchesRequest
                {
                    ProfileId = profileId.ToString(),
                    PhoneE164 = phoneE164
                },
                BuildHeaders(), Deadline(), ct);
            return response.Matches;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
        {
            // Paridad con el puente REST anterior: 404/400 desde iam => no match.
            return false;
        }
    }

    public async Task UpdatePasswordAsync(Guid profileId, string newPassword, CancellationToken ct)
    {
        try
        {
            await _client.UpdatePasswordAsync(
                new UpdatePasswordRequest
                {
                    ProfileId = profileId.ToString(),
                    NewPassword = newPassword
                },
                BuildHeaders(), Deadline(), ct);
        }
        catch (RpcException ex)
        {
            LogFailure(ex, "update password", profileId);
            throw;
        }
    }

    public async Task UpdateEmailAsync(Guid profileId, string newEmail, CancellationToken ct)
    {
        try
        {
            await _client.UpdateEmailAsync(
                new UpdateEmailRequest
                {
                    ProfileId = profileId.ToString(),
                    NewEmail = newEmail
                },
                BuildHeaders(), Deadline(), ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            throw new EmailAlreadyInUseException();
        }
        catch (RpcException ex)
        {
            LogFailure(ex, "update email", profileId);
            throw;
        }
    }

    public async Task UpdatePhoneAsync(Guid profileId, string newPhoneE164, CancellationToken ct)
    {
        try
        {
            await _client.UpdatePhoneAsync(
                new UpdatePhoneRequest
                {
                    ProfileId = profileId.ToString(),
                    NewPhoneE164 = newPhoneE164
                },
                BuildHeaders(), Deadline(), ct);
        }
        catch (RpcException ex)
        {
            LogFailure(ex, "update phone", profileId);
            throw;
        }
    }

    private Metadata BuildHeaders()
    {
        var headers = new Metadata();
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            headers.Add(ApiKeyHeader, _apiKey);
        }
        return headers;
    }

    private static DateTime Deadline() => DateTime.UtcNow.Add(Timeout);

    private void LogFailure(RpcException ex, string operation, Guid profileId)
    {
        _logger.LogError(
            "Upstream {Operation} failed for profile {ProfileId} with gRPC status {Status}",
            operation, profileId, ex.StatusCode);
    }

    public void Dispose() => _channel?.Dispose();
}
