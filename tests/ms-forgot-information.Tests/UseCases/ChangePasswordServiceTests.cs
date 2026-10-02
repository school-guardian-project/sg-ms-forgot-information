using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.Password.Application.Dto;
using ms_forgot_information.Api.Password.Application.UseCase;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Tests.Fakes;
using Xunit;

namespace ms_forgot_information.Tests.UseCases;

public class ChangePasswordServiceTests
{
    private const string StrongPassword = "Str0ng!Pass";

    private static (ChangePasswordService UseCase, FakeIdentityDirectoryClient Identity) Build()
    {
        var repo = new FakeVerificationRequestRepository();
        var identity = new FakeIdentityDirectoryClient();
        var verificationCodeService = new VerificationCodeService(
            repo, new FakeEmailSender(), new FakeSmsSender(), new SecretHasher("pepper"),
            Options.Create(new OtpOptions { MaxRequestsPerWindow = 3, RequestWindowMinutes = 10, MaxAttempts = 5, ExpirationMinutes = 10, ResetTokenExpirationMinutes = 5, CodeLength = 6 }),
            NullLogger<VerificationCodeService>.Instance,
            TimeProvider.System);

        var useCase = new ChangePasswordService(identity, verificationCodeService, new FakeDomainEventPublisher());
        return (useCase, identity);
    }

    [Fact]
    public async Task Throws_when_neither_current_password_nor_code_is_supplied()
    {
        var (useCase, _) = Build();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => useCase.ExecuteAsync(
            Guid.NewGuid(), new ChangePasswordRequestDto(null, null, StrongPassword, StrongPassword), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_both_current_password_and_code_are_supplied()
    {
        var (useCase, _) = Build();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => useCase.ExecuteAsync(
            Guid.NewGuid(), new ChangePasswordRequestDto("oldPassword", "123456", StrongPassword, StrongPassword), CancellationToken.None));
    }

    [Fact]
    public async Task Succeeds_with_a_valid_current_password_only()
    {
        var (useCase, identity) = Build();
        identity.CurrentPasswordValid = true;
        var profileId = Guid.NewGuid();

        await useCase.ExecuteAsync(profileId, new ChangePasswordRequestDto("oldPassword", null, StrongPassword, StrongPassword), CancellationToken.None);

        Assert.Equal(StrongPassword, identity.LastPasswordUpdate);
    }

    [Fact]
    public async Task Rejects_a_weak_new_password()
    {
        var (useCase, identity) = Build();
        identity.CurrentPasswordValid = true;

        await Assert.ThrowsAsync<WeakPasswordException>(() => useCase.ExecuteAsync(
            Guid.NewGuid(), new ChangePasswordRequestDto("oldPassword", null, "weak", "weak"), CancellationToken.None));
    }
}
