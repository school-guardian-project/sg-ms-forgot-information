using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.Shared.Infrastructure.Notifications;

/// <summary>
/// Sends SMS through Twilio's public REST API (no SDK dependency needed — a single POST).
/// https://www.twilio.com/docs/sms/send-messages
/// </summary>
public class TwilioSmsSender(IHttpClientFactory httpClientFactory, IOptions<SmsOptions> options) : ISmsSender
{
    private const string ClientName = "twilio";
    private readonly SmsOptions _options = options.Value;

    public async Task SendAsync(string toPhoneE164, string message, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.BaseAddress = new Uri("https://api.twilio.com/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}")));

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = toPhoneE164,
            ["From"] = _options.FromNumber,
            ["Body"] = message
        });

        var response = await client.PostAsync(
            $"2010-04-01/Accounts/{_options.AccountSid}/Messages.json", form, ct);

        response.EnsureSuccessStatusCode();
    }
}
