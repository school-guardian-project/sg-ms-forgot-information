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
using NUnit.Framework;

namespace ms_forgot_information.Tests.Infrastructure;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class IdentityDirectoryGrpcClientTests
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

    [SetUp]
    public async Task SetUp()
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

    [TearDown]
    public async Task TearDown()
    {
        _channel.Dispose();
        await _server.StopAsync();
        await _server.DisposeAsync();
    }

    private IdentityDirectoryGrpcClient BuildClient(string? apiKey = "test-key") =>
        new(
            new IdentityDirectoryOptions { GrpcAddress = _server.Urls.First(), IamApiKey = apiKey ?? string.Empty },
            NullLogger<IdentityDirectoryGrpcClient>.Instance);

    [Test]
    public async Task Resolves_the_email_to_the_profile_returned_by_iam()
    {
        var id = Guid.NewGuid();
        _stub.OnFindProfileByEmail = req => new ProfileLookup
        {
            ProfileId = id.ToString(),
            Email = req.Email
        };

        var profile = await BuildClient().FindProfileByEmailAsync("a+b@gmail.com", CancellationToken.None);

        Assert.That(profile!.ProfileId, Is.EqualTo(id));
        Assert.That(profile.Email, Is.EqualTo("a+b@gmail.com"));
    }

    [Test]
    public async Task Returns_null_when_iam_answers_not_found()
    {
        _stub.OnFindProfileByEmail = null;

        Assert.That(await BuildClient().FindProfileByEmailAsync("nobody@gmail.com", CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task Sends_the_internal_api_key_and_the_new_password_to_iam()
    {
        var id = Guid.NewGuid();

        await BuildClient(apiKey: "test-key").UpdatePasswordAsync(id, "NewPassw0rd!", CancellationToken.None);

        Assert.That(_stub.LastPasswordRequest!.ProfileId, Is.EqualTo(id.ToString()));
        Assert.That(_stub.LastPasswordRequest.NewPassword, Is.EqualTo("NewPassw0rd!"));
        Assert.That(_stub.LastHeaders!.GetValue("x-internal-api-key"), Is.EqualTo("test-key"));
    }

    [Test]
    public async Task Forwards_the_new_phone_in_e164()
    {
        var id = Guid.NewGuid();

        await BuildClient().UpdatePhoneAsync(id, "+573001234567", CancellationToken.None);

        Assert.That(_stub.LastPhoneRequest!.NewPhoneE164, Is.EqualTo("+573001234567"));
    }

    [Test]
    public async Task Phone_match_flag_comes_from_iam()
    {
        _stub.OnPhoneMatches = _ => new PhoneMatchesResponse { Matches = true };

        Assert.That(await BuildClient().PhoneMatchesAsync(Guid.NewGuid(), "+573001234567", CancellationToken.None), Is.True);
    }

    [Test]
    public async Task Invalid_or_missing_phone_data_from_iam_counts_as_no_match()
    {
        _stub.OnPhoneMatches = null; // stub answers NOT_FOUND

        Assert.That(await BuildClient().PhoneMatchesAsync(Guid.NewGuid(), "+573001234567", CancellationToken.None), Is.False);
    }

    [Test]
    public void Email_conflict_from_iam_surfaces_as_EmailAlreadyInUseException()
    {
        _stub.OnUpdateEmail = _ =>
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Email already in use"));

        Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => BuildClient().UpdateEmailAsync(Guid.NewGuid(), "taken@gmail.com", CancellationToken.None));
    }

    [Test]
    public void Rejected_credentials_from_iam_surface_as_an_error()
    {
        _stub.OnFindProfileByEmail = _ =>
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid internal API key"));

        Assert.ThrowsAsync<RpcException>(
            () => BuildClient().FindProfileByEmailAsync("a@b.com", CancellationToken.None));
    }
}
