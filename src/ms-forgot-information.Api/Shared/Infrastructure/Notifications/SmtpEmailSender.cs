using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Port.Out;

namespace ms_forgot_information.Api.Shared.Infrastructure.Notifications;

/// <summary>
/// SMTP delivery through the configured provider (see Smtp:* settings).
/// Port 587 + EnableSsl=true negotiates STARTTLS; implicit TLS (port 465) is not supported by SmtpClient.
/// Failures surface as <see cref="NotificationDeliveryException"/>; credentials and message content are never logged.
/// </summary>
public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                throw new InvalidOperationException("Smtp:Host and Smtp:FromAddress must be configured.");
            }

            var timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Timeout = (int)timeout.TotalMilliseconds
            };

            // Only authenticate when SMTP credentials are configured.
            if (!string.IsNullOrEmpty(_options.User))
            {
                client.Credentials = new System.Net.NetworkCredential(_options.User, _options.Password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            message.To.Add(toEmail);
            logger.LogInformation("SMTP sending: from {From} to {To}", EmailLogMask.Mask(_options.FromAddress), EmailLogMask.Mask(toEmail));

            await client.SendMailAsync(message, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP delivery failed (host {Host}, port {Port})", _options.Host, _options.Port);
            throw new NotificationDeliveryException(ex);
        }
    }
}