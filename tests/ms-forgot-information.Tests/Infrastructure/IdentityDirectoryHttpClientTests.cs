using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using ms_forgot_information.Api.Shared.Infrastructure.Clients;
using NUnit.Framework;

namespace ms_forgot_information.Tests.Infrastructure;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class IdentityDirectoryHttpClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static (IdentityDirectoryHttpClient Client, StubHandler Handler) Build(Func<HttpRequestMessage, HttpResponseMessage> respond, string? apiKey = null)
    {
        var handler = new StubHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://iam.test") };
        if (apiKey is not null)
        {
            http.DefaultRequestHeaders.Add("X-Internal-Api-Key", apiKey);
        }
        return (new IdentityDirectoryHttpClient(new SingleClientFactory(http), NullLogger<IdentityDirectoryHttpClient>.Instance), handler);
    }

    [Test]
    public async Task Resolves_any_email_to_the_profile_returned_by_iam_and_escapes_the_query()
    {
        var id = Guid.NewGuid();
        var (client, handler) = Build(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"profileId\":\"{id}\",\"email\":\"a+b@gmail.com\"}}", Encoding.UTF8, "application/json")
        });

        var profile = await client.FindProfileByEmailAsync("a+b@gmail.com", CancellationToken.None);

        Assert.That(profile!.ProfileId, Is.EqualTo(id));
        Assert.That(profile.Email, Is.EqualTo("a+b@gmail.com"));
        Assert.That(handler.LastRequest!.RequestUri!.PathAndQuery, Is.EqualTo("/api/profiles/by-email?email=a%2Bb%40gmail.com"));
    }

    [Test]
    public async Task Returns_null_when_iam_answers_404()
    {
        var (client, _) = Build(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.That(await client.FindProfileByEmailAsync("nobody@gmail.com", CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task Sends_the_internal_api_key_and_the_new_password_to_iam()
    {
        var id = Guid.NewGuid();
        var (client, handler) = Build(_ => new HttpResponseMessage(HttpStatusCode.OK), apiKey: "test-key");

        await client.UpdatePasswordAsync(id, "NewPassw0rd!", CancellationToken.None);

        Assert.That(handler.LastRequest!.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(handler.LastRequest.RequestUri!.AbsolutePath, Is.EqualTo($"/api/profiles/{id}/password"));
        Assert.That(handler.LastRequest.Headers.GetValues("X-Internal-Api-Key").Single(), Is.EqualTo("test-key"));
        Assert.That(handler.LastBody, Does.Contain("\"password\":\"NewPassw0rd!\""));
    }

    [Test]
    public async Task Rejected_credentials_from_iam_surface_as_an_error()
    {
        var (client, _) = Build(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        Assert.ThrowsAsync<HttpRequestException>(() => client.FindProfileByEmailAsync("a@b.com", CancellationToken.None));
    }

}
