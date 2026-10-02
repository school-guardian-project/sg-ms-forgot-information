using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Tests.Fakes;
using Xunit;

namespace ms_forgot_information.Tests.Services;

public class VerificationCodeServiceTests
{
    private static (VerificationCodeService Service, FakeVerificationRequestRepository Repo, FakeEmailSender Email, ManualTimeProvider Clock) Build(OtpOptions? options = null)
    {
        var repo = new FakeVerificationRequestRepository();
        var email = new FakeEmailSender();
        var sms = new FakeSmsSender();
        var hasher = new SecretHasher("test-pepper");
        var clock = new ManualTimeProvider();

        var service = new VerificationCodeService(
            repo, email, sms, hasher,
            Options.Create(options ?? new OtpOptions { CodeLength = 6, ExpirationMinutes = 10, MaxAttempts = 3, MaxRequestsPerWindow = 3, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 }),
            NullLogger<VerificationCodeService>.Instance,
            clock);

        return (service, repo, email, clock);
    }

    [Fact]
    public async Task IssueAsync_sends_code_by_email_when_target_is_an_email()
    {
        var (service, repo, email, _) = Build();
        var profileId = Guid.NewGuid();

        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        Assert.Equal(1, repo.Count);
        Assert.Single(email.Sent);
        Assert.Equal("user@example.com", email.Sent[0].To);
    }

    [Fact]
    public async Task VerifyAsync_succeeds_with_the_correct_code()
    {
        var (service, repo, email, _) = Build();
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        var code = ExtractCode(email.Sent[0].Body);
        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None);

        Assert.Equal(profileId, ticket.ProfileId);
        Assert.False(string.IsNullOrEmpty(ticket.ResetToken));
    }

    [Fact]
    public async Task VerifyAsync_throws_InvalidCodeException_for_a_wrong_code()
    {
        var (service, _, email, _) = Build();
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCodeException>(
            () => service.VerifyAsync(profileId, Purpose.PasswordReset, "000000", CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsync_locks_after_reaching_MaxAttempts()
    {
        var (service, _, email, _) = Build(new OtpOptions { CodeLength = 6, ExpirationMinutes = 10, MaxAttempts = 2, MaxRequestsPerWindow = 5, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 });
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCodeException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, "000000", CancellationToken.None));
        await Assert.ThrowsAsync<TooManyAttemptsException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, "000000", CancellationToken.None));

        var correctCode = ExtractCode(email.Sent[0].Body);
        await Assert.ThrowsAsync<TooManyAttemptsException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, correctCode, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsync_throws_CodeExpiredException_once_expiration_elapses()
    {
        var (service, _, email, clock) = Build(new OtpOptions { CodeLength = 6, ExpirationMinutes = 10, MaxAttempts = 3, MaxRequestsPerWindow = 5, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 });
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var code = ExtractCode(email.Sent[0].Body);

        clock.Advance(TimeSpan.FromMinutes(11));

        await Assert.ThrowsAsync<CodeExpiredException>(() => service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None));
    }

    [Fact]
    public async Task IssueAsync_throws_TooManyRequestsException_beyond_the_window_limit()
    {
        var (service, _, _, _) = Build(new OtpOptions { CodeLength = 6, ExpirationMinutes = 10, MaxAttempts = 3, MaxRequestsPerWindow = 2, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 });
        var profileId = Guid.NewGuid();

        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        await Assert.ThrowsAsync<TooManyRequestsException>(
            () => service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task A_consumed_code_cannot_be_reused()
    {
        var (service, _, email, _) = Build();
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var code = ExtractCode(email.Sent[0].Body);

        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None);
        await service.CompleteAsync(ticket.RequestId, CancellationToken.None);

        // No more active request for this profile+purpose once consumed.
        await Assert.ThrowsAsync<InvalidCodeException>(
            () => service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None));
    }

    private static string ExtractCode(string body)
    {
        var start = body.IndexOf("es: ", StringComparison.Ordinal) + 4;
        var end = body.IndexOf('.', start);
        return body[start..end];
    }
}
