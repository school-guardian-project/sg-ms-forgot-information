using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Application.UseCase;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Tests.Fakes;
using Xunit;

namespace ms_forgot_information.Tests.Services;

public class EmailDeliveryAndResetTokenTests
{
    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct) =>
            throw new NotificationDeliveryException(new InvalidOperationException("smtp down"));
    }

    private static (VerificationCodeService Service, FakeVerificationRequestRepository Repo, ManualTimeProvider Clock) Build(IEmailSender email)
    {
        var repo = new FakeVerificationRequestRepository();
        var clock = new ManualTimeProvider();
        var service = new VerificationCodeService(
            repo, email, new SecretHasher("test-pepper"),
            Options.Create(new OtpOptions { ExpirationMinutes = 10, MaxAttempts = 3, MaxRequestsPerWindow = 3, RequestWindowMinutes = 10, ResetTokenExpirationMinutes = 5 }),
            NullLogger<VerificationCodeService>.Instance,
            clock);
        return (service, repo, clock);
    }

    private static string ExtractCode(FakeEmailSender email) => Regex.Match(email.Sent[0].Body, @"\d{6}").Value;

    [Fact]
    public async Task A_failed_delivery_throws_and_burns_the_code()
    {
        var (service, repo, _) = Build(new FailingEmailSender());
        var profileId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotificationDeliveryException>(
            () => service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None));

        Assert.Equal(1, repo.Count);
        // The code never reached the user, so no code (not even a guessed one) may be accepted afterwards.
        await Assert.ThrowsAsync<InvalidCodeException>(
            () => service.VerifyAsync(profileId, Purpose.PasswordReset, "000000", CancellationToken.None));
    }

    [Fact]
    public async Task A_valid_reset_token_is_accepted_and_a_wrong_one_is_rejected()
    {
        var email = new FakeEmailSender();
        var (service, _, _) = Build(email);
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);

        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, ExtractCode(email), CancellationToken.None);

        var consumed = await service.ConsumeResetTokenAsync(profileId, Purpose.PasswordReset, ticket.ResetToken, CancellationToken.None);
        Assert.Equal(profileId, consumed.ProfileId);
        await Assert.ThrowsAsync<InvalidCodeException>(
            () => service.VerifyAsync(profileId, Purpose.PasswordReset, ExtractCode(email), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidResetTokenException>(
            () => service.ConsumeResetTokenAsync(profileId, Purpose.PasswordReset, "wrong-token", CancellationToken.None));
    }

    [Fact]
    public async Task Reset_updates_iam_then_consumes_the_reset_token()
    {
        var email = new FakeEmailSender();
        var (service, _, _) = Build(email);
        var identity = new FakeIdentityDirectoryClient();
        var profileId = Guid.NewGuid();
        identity.ProfilesByEmail["user@example.com"] = profileId;

        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, ExtractCode(email), CancellationToken.None);
        var reset = new ResetPasswordService(identity, service);
        var request = new ResetPasswordRequestDto("USER@example.com", ticket.ResetToken, "NewPassw0rd!", "NewPassw0rd!");

        await reset.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal("NewPassw0rd!", identity.LastPasswordUpdate);
        await Assert.ThrowsAsync<InvalidResetTokenException>(() => reset.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Reset_token_remains_valid_when_iam_update_fails()
    {
        var email = new FakeEmailSender();
        var (service, _, _) = Build(email);
        var identity = new FakeIdentityDirectoryClient { ThrowOnNextUpdate = true };
        var profileId = Guid.NewGuid();
        identity.ProfilesByEmail["user@example.com"] = profileId;

        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, ExtractCode(email), CancellationToken.None);
        var reset = new ResetPasswordService(identity, service);
        var request = new ResetPasswordRequestDto("user@example.com", ticket.ResetToken, "NewPassw0rd!", "NewPassw0rd!");

        await Assert.ThrowsAsync<UpstreamUpdateException>(() => reset.ExecuteAsync(request, CancellationToken.None));

        identity.ThrowOnNextUpdate = false;
        await reset.ExecuteAsync(request, CancellationToken.None);
        Assert.Equal("NewPassw0rd!", identity.LastPasswordUpdate);
    }

    [Fact]
    public async Task An_expired_reset_token_is_rejected()
    {
        var email = new FakeEmailSender();
        var (service, _, clock) = Build(email);
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, ExtractCode(email), CancellationToken.None);

        clock.Advance(TimeSpan.FromMinutes(6));

        await Assert.ThrowsAsync<InvalidResetTokenException>(
            () => service.ConsumeResetTokenAsync(profileId, Purpose.PasswordReset, ticket.ResetToken, CancellationToken.None));
    }

    [Fact]
    public async Task A_used_code_cannot_be_verified_again_after_completion()
    {
        var email = new FakeEmailSender();
        var (service, _, _) = Build(email);
        var profileId = Guid.NewGuid();
        await service.IssueAsync(profileId, Purpose.PasswordReset, "user@example.com", "127.0.0.1", CancellationToken.None);
        var code = ExtractCode(email);
        var ticket = await service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None);
        await service.CompleteAsync(ticket.RequestId, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCodeException>(
            () => service.VerifyAsync(profileId, Purpose.PasswordReset, code, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidResetTokenException>(
            () => service.ConsumeResetTokenAsync(profileId, Purpose.PasswordReset, ticket.ResetToken, CancellationToken.None));
    }

    [Fact]
    public async Task Forgot_password_gives_the_same_outcome_for_known_unknown_and_undeliverable_emails()
    {
        var identity = new FakeIdentityDirectoryClient();
        identity.ProfilesByEmail["known@example.com"] = Guid.NewGuid();
        var (service, repo, _) = Build(new FailingEmailSender());
        var useCase = new ForgotPasswordService(identity, service, NullLogger<ForgotPasswordService>.Instance);

        var known = await Record.ExceptionAsync(() => useCase.ExecuteAsync(new ForgotPasswordRequestDto("known@example.com"), "127.0.0.1", CancellationToken.None));
        var unknown = await Record.ExceptionAsync(() => useCase.ExecuteAsync(new ForgotPasswordRequestDto("unknown@example.com"), "127.0.0.1", CancellationToken.None));

        Assert.Null(known);
        Assert.Null(unknown);
        Assert.Equal(1, repo.Count);
    }
}