namespace ms_forgot_information.Api.Shared.Domain.Port.Out;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);

    /// <summary>Sends a multipart email (plain text + HTML). Senders without HTML support fall back to the text part.</summary>
    Task SendAsync(string toEmail, string subject, string textBody, string htmlBody, CancellationToken ct)
        => SendAsync(toEmail, subject, textBody, ct);
}