using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Infrastructure.Grpc;
using Xunit;

namespace ms_forgot_information.Tests.Infrastructure;

public class IdentityDirectoryGrpcClientTests : IAsyncLifetime
{
    private sealed class StubInternalProfileService : InternalProfileService.InternalProfileServiceBase
    {
        public Func<FindProfileByEmailRequest, ProfileLookup>? OnFindProfileByEmail { get; set; }
        public Func<PhoneMatchesRequest, PhoneMatchesResponse>? OnPhoneMatches { get; set; }
        public Func<UpdateEmailRequest, UpdateEmailResponse>? OnUpdateEmail { get; set; }

        public Metadata? LastHeaders { get; private set; }
        public UpdatePasswordRequest? LastPasswordRequest { get; private set; }
        public UpdatePhoneRequest? LastPhoneRequest { get; private set; }

        public override Task<ProfileLookup> FindProfileByEmail(
            FindProfileByEmailRequest request, ServerCallContext context)
        {
            LastHeaders = context.RequestHeaders;
            if (OnFindProfileByEmail is null) throw new RpcException(new Status(StatusCode.NotFound, "not found"));
            return Task.FromResult(OnFindProfileByEmail(request));
        }

        public override Task<PhoneMatchesResponse> PhoneMatches(
            PhoneMatchesRequest request, ServerCallContext context)
        {
            LastHeaders = context.RequestHeaders;
            if (OnPhoneMatches is null) throw new RpcException(new Status(StatusCode.NotFound, "not found"));
            return Task.FromResult(OnPhoneMatches(request));
        }

        public override Task<UpdatePasswordResponse> UpdatePassword(
            UpdatePasswordRequest request, ServerCallContext context)
        {
            LastHeaders = context.RequestHeaders;
            LastPasswordRequest = request;
            return Task.FromResult(new UpdatePasswordResponse());
        }

        public override Task<UpdateEmailResponse> UpdateEmail(
            UpdateEmailRequest request, ServerCallContext context)
        {
            LastHeaders = context.RequestHeaders;
            if (OnUpdateEmail is not null) return Task.FromResult(OnUpdateEmail(request));
            return Task.FromResult(new UpdateEmailResponse());
        }

        public override Task<UpdatePhoneResponse> UpdatePhone(
            UpdatePhoneRequest request, ServerCallContext context)
        {
            LastHeaders = context.RequestHeaders;
            LastPhoneRequest = request;
            return Task.FromResult(new UpdatePhoneResponse());
        }
    }

    private readonly StubInternalProfileService _stub = new();
    private WebApplication _server = null!;
    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(k =>
            k.ListenLocalhost(0, o => o.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_stub);
        _server = builder.Build();
        _server.MapGrpcService<StubInternalProfileService>();
        await _server.StartAsync();
        _channel = GrpcChannel.ForAddress(_server.Urls.First());
    }

    public async Task DisposeAsync()
    {
        _channel.Dispose();
        await _server.StopAsync();
        await _server.DisposeAsync();
    }

    private IdentityDirectoryGrpcClient BuildClient(string? apiKey = "test-key") =>
        new(
            new IdentityDirectoryOptions { GrpcAddress = _server.Urls.First(), IamApiKey = apiKey ?? string.Empty },
            NullLogger<IdentityDirectoryGrpcClient>.Instance);

    [Fact]
    public async Task Resolves_the_email_to_the_profile_returned_by_iam()
    {
        var id = Guid.NewGuid();
        _stub.OnFindProfileByEmail = req => new ProfileLookup
        {
            ProfileId = id.ToString(),
            Email = req.Email
        };

        var profile = await BuildClient().FindProfileByEmailAsync("a+b@gmail.com", CancellationToken.None);

        Assert.Equal(id, profile!.ProfileId);
        Assert.Equal("a+b@gmail.com", profile.Email);
    }

    [Fact]
    public async Task Returns_null_when_iam_answers_not_found()
    {
        _stub.OnFindProfileByEmail = null;

        Assert.Null(await BuildClient().FindProfileByEmailAsync("nobody@gmail.com", CancellationToken.None));
    }

    [Fact]
    public async Task Sends_the_internal_api_key_and_the_new_password_to_iam()
    {
        var id = Guid.NewGuid();

        await BuildClient(apiKey: "test-key").UpdatePasswordAsync(id, "NewPassw0rd!", CancellationToken.None);

        Assert.Equal(id.ToString(), _stub.LastPasswordRequest!.ProfileId);
        Assert.Equal("NewPassw0rd!", _stub.LastPasswordRequest.NewPassword);
        Assert.Equal("test-key", _stub.LastHeaders!.GetValue("x-internal-api-key"));
    }

    [Fact]
    public async Task Forwards_the_new_phone_in_e164()
    {
        var id = Guid.NewGuid();

        await BuildClient().UpdatePhoneAsync(id, "+573001234567", CancellationToken.None);

        Assert.Equal("+573001234567", _stub.LastPhoneRequest!.NewPhoneE164);
    }

    [Fact]
    public async Task Phone_match_flag_comes_from_iam()
    {
        _stub.OnPhoneMatches = _ => new PhoneMatchesResponse { Matches = true };

        Assert.True(await BuildClient().PhoneMatchesAsync(Guid.NewGuid(), "+573001234567", CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_or_missing_phone_data_from_iam_counts_as_no_match()
    {
        _stub.OnPhoneMatches = null; // stub answers NOT_FOUND

        Assert.False(await BuildClient().PhoneMatchesAsync(Guid.NewGuid(), "+573001234567", CancellationToken.None));
    }

    [Fact]
    public async Task Email_conflict_from_iam_surfaces_as_EmailAlreadyInUseException()
    {
        _stub.OnUpdateEmail = _ =>
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Email already in use"));

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => BuildClient().UpdateEmailAsync(Guid.NewGuid(), "taken@gmail.com", CancellationToken.None));
    }

    [Fact]
    public async Task Rejected_credentials_from_iam_surface_as_an_error()
    {
        _stub.OnFindProfileByEmail = _ =>
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid internal API key"));

        await Assert.ThrowsAsync<RpcException>(
            () => BuildClient().FindProfileByEmailAsync("a@b.com", CancellationToken.None));
    }
}
