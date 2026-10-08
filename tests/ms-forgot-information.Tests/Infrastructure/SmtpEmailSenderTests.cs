using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Infrastructure.Notifications;
using Xunit;

namespace ms_forgot_information.Tests.Infrastructure;

public class SmtpEmailSenderTests
{
    /// <summary>Minimal in-process SMTP server (MailHog stand-in): accepts one message and records it.</summary>
    private sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public readonly TaskCompletionSource<string> Received = new();

        public FakeSmtpServer()
        {
            _listener.Start();
            _ = Task.Run(Serve);
        }

        private async Task Serve()
        {
            using var client = await _listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            await writer.WriteLineAsync("220 fake-smtp ready");
            var data = new StringBuilder();
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("354 go ahead");
                    while ((line = await reader.ReadLineAsync()) is not null && line != ".")
                    {
                        data.AppendLine(line);
                    }
                    await writer.WriteLineAsync("250 queued");
                }
                else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }

            Received.TrySetResult(data.ToString());
        }

        public void Dispose() => _listener.Stop();
    }

    private static SmtpEmailSender Build(SmtpOptions options) =>
        new(Options.Create(options), NullLogger<SmtpEmailSender>.Instance);

    [Fact]
    public async Task Delivers_the_message_to_an_SMTP_server_without_credentials()
    {
        using var server = new FakeSmtpServer();
        var sender = Build(new SmtpOptions { Host = "127.0.0.1", Port = server.Port, EnableSsl = false, FromAddress = "no-reply@example.test", FromName = "Test", TimeoutSeconds = 5 });

        await sender.SendAsync("anyone@example.org", "Verification code", "Your code is 123456", CancellationToken.None);

        var data = await server.Received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("anyone@example.org", data);
        Assert.Contains("charset=utf-8", data, StringComparison.OrdinalIgnoreCase);

        var bodyStart = data.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4;
        var body = Encoding.UTF8.GetString(Convert.FromBase64String(data[bodyStart..].Trim()));
        Assert.Contains("Your code is 123456", body);
    }

    [Fact]
    public async Task Wraps_connection_failures_in_NotificationDeliveryException()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var closedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var sender = Build(new SmtpOptions { Host = "127.0.0.1", Port = closedPort, EnableSsl = false, FromAddress = "no-reply@example.test", TimeoutSeconds = 2 });

        await Assert.ThrowsAsync<NotificationDeliveryException>(
            () => sender.SendAsync("anyone@example.org", "s", "b", CancellationToken.None));
    }

    [Fact]
    public async Task Fails_with_NotificationDeliveryException_when_smtp_is_not_configured()
    {
        var sender = Build(new SmtpOptions());

        await Assert.ThrowsAsync<NotificationDeliveryException>(
            () => sender.SendAsync("anyone@example.org", "s", "b", CancellationToken.None));
    }
}