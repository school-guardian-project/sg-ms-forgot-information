using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Password.Application.UseCase;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Tests.Fakes;
using Xunit;

namespace ms_forgot_information.Tests.UseCases;

public class ForgotPasswordServiceTests
{
    private static (ForgotPasswordService UseCase, FakeVerificationRequestRepository Repo, FakeIdentityDirectoryClient Identity) Build()
    {
        var repo = new FakeVerificationRequestRepository();
        var identity = new FakeIdentityDirectoryClient();
        var verificationCodeService = new VerificationCodeService(
            repo, new FakeEmailSender(), new SecretHasher("pepper"),
            Options.Create(new OtpOptions { MaxRequestsPerWindow = 3, RequestWindowMinutes = 10, MaxAttempts = 5, ExpirationMinutes = 10, ResetTokenExpirationMinutes = 5 }),
            NullLogger<VerificationCodeService>.Instance,
            TimeProvider.System);

        var useCase = new ForgotPasswordService(identity, verificationCodeService, NullLogger<ForgotPasswordService>.Instance);
        return (useCase, repo, identity);
    }

    [Fact]
    public async Task Reports_account_not_found_and_issues_no_code_when_the_email_does_not_exist()
    {
        var (useCase, repo, _) = Build();

        await Assert.ThrowsAsync<AccountNotFoundException>(
            () => useCase.ExecuteAsync(new ForgotPasswordRequestDto("unknown@example.com"), "127.0.0.1", CancellationToken.None));
        Assert.Equal(0, repo.Count);
    }

    [Fact]
    public async Task Issues_a_code_when_the_email_exists()
    {
        var (useCase, repo, identity) = Build();
        var profileId = Guid.NewGuid();
        identity.ProfilesByEmail["known@example.com"] = profileId;

        await useCase.ExecuteAsync(new ForgotPasswordRequestDto("known@example.com"), "127.0.0.1", CancellationToken.None);

        Assert.Equal(1, repo.Count);
    }
}
