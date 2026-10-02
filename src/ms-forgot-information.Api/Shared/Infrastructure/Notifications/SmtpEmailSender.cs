using System.Net.Mail;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.Shared.Infrastructure.Notifications;

/// <summary>Real SMTP delivery. Configure Smtp:* via environment variables/.env — never hardcode credentials.</summary>
public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        // Local test SMTP servers (e.g. MailHog) accept anonymous delivery — only authenticate when credentials are configured.
        if (!string.IsNullOrEmpty(_options.User))
        {
            client.Credentials = new System.Net.NetworkCredential(_options.User, _options.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message, ct);
    }
}
