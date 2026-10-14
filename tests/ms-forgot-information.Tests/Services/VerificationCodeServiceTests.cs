using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Tests.Fakes;
using NUnit.Framework;

namespace ms_forgot_information.Tests.Services;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class VerificationCodeServiceTests
{
    private static (VerificationCodeService Service, FakeVerificationRequestRepository Repo, FakeEmailSender Email, ManualTimeProvider Clock) Build(OtpOptions? options = null)
    {
        var repo = new FakeVerificationRequestRepository();
        var email = new FakeEmailSender();
        var hasher = new SecretHasher("test-pepper");
        var clock = new ManualTimeProvider();

        var service = new VerificationCodeService(
            repo, email, hasher,
            Options.Create(options ?? new OtpOptions { ExpirationMinutes = 10, MaxAttempts = 3, MaxRequestsPerWindow = 3, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 }),
            NullLogger<VerificationCodeService>.Instance,
            clock);

        return (service, repo, email, clock);
    }

    [Test]
    public async Task IssueAsync_sends_code_by_email_when_target_is_an_email()
    {
        var (service, repo, email, _) = Build();
        var profileId = Guid.NewGuid();

        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        Assert.That(repo.Count, Is.EqualTo(1));
        Assert.That(email.Sent, Has.Exactly(1).Items);
        Assert.That(email.Sent[0].To, Is.EqualTo("user@example.com"));
    }

    [Test]
    public async Task VerifyAsync_succeeds_with_the_correct_code()
    {
        var (service, repo, email, _) = Build();
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        var code = ExtractCode(email.Sent[0].Body);
        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None);

        Assert.That(ticket.ProfileId, Is.EqualTo(profileId));
        Assert.That(string.IsNullOrEmpty(ticket.ResetToken), Is.False);
    }

    [Test]
    public async Task VerifyAsync_throws_InvalidCodeException_for_a_wrong_code()
    {
        var (service, _, email, _) = Build();
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        Assert.ThrowsAsync<InvalidCodeException>(
            () => service.VerifyAsync(profileId, Purpose.PasswordReset, "000000", CancellationToken.None));
    }

    [Test]
    public async Task VerifyAsync_locks_after_reaching_MaxAttempts()
    {
        var (service, _, email, _) = Build(new OtpOptions { ExpirationMinutes = 10, MaxAttempts = 2, MaxRequestsPerWindow = 5, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 });
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        Assert.ThrowsAsync<InvalidCodeException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, "000000", CancellationToken.None));
        Assert.ThrowsAsync<TooManyAttemptsException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, "000000", CancellationToken.None));

        var correctCode = ExtractCode(email.Sent[0].Body);
        Assert.ThrowsAsync<TooManyAttemptsException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, correctCode, CancellationToken.None));
    }

    [Test]
    public async Task VerifyAsync_throws_CodeExpiredException_once_expiration_elapses()
    {
        var (service, _, email, clock) = Build(new OtpOptions { ExpirationMinutes = 10, MaxAttempts = 3, MaxRequestsPerWindow = 5, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 });
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var code = ExtractCode(email.Sent[0].Body);

        clock.Advance(TimeSpan.FromMinutes(11));

        Assert.ThrowsAsync<CodeExpiredException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None));
    }

    [Test]
    public async Task IssueAsync_throws_TooManyRequestsException_beyond_the_window_limit()
    {
        var (service, _, _, _) = Build(new OtpOptions { ExpirationMinutes = 10, MaxAttempts = 3, MaxRequestsPerWindow = 2, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 });
        var profileId = Guid.NewGuid();

        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        Assert.ThrowsAsync<TooManyRequestsException>(
            () => service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None));
    }

    [Test]
    public async Task A_consumed_code_cannot_be_reused()
    {
        var (service, _, email, _) = Build();
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var code = ExtractCode(email.Sent[0].Body);

        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None);
        await service.CompleteAsync(ticket.RequestId, CancellationToken.None);

        // No more active request for this profile+purpose once consumed.
        Assert.ThrowsAsync<InvalidCodeException>(
            () => service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None));
    }

    private static string ExtractCode(string body)
    {
        return System.Text.RegularExpressions.Regex.Match(body, @"\d{6}").Value;
    }
}
