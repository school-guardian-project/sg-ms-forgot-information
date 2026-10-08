using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_forgot_information.Api.PhoneChange.Application.Dto;
using ms_forgot_information.Api.PhoneChange.Application.UseCase;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Application.Validation;
using ms_forgot_information.Api.Shared.Domain.Exceptions;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Infrastructure.Notifications;
using ms_forgot_information.Tests.Fakes;
using Xunit;

namespace ms_forgot_information.Tests.UseCases;

public class PhoneChangeTests
{
    private const string Email = "owner@example.com";
    private const string NewPhone = "+573001234567";
    private const string CurrentPhone = "+573119998877";

    private readonly FakeVerificationRequestRepository _repo = new();
    private readonly FakeIdentityDirectoryClient _identity = new();
    private readonly FakeEmailSender _email = new();
    private readonly FakeSmsVerificationService _sms = new();
    private readonly VerificationCodeService _codes;
    private readonly Guid _profileId = Guid.NewGuid();

    public PhoneChangeTests()
    {
        _identity.ProfilesByEmail[Email] = _profileId;
        _identity.StoredPhones[_profileId] = CurrentPhone;
        _codes = new VerificationCodeService(
            _repo, _email, new SecretHasher("pepper"),
            Options.Create(new OtpOptions { MaxRequestsPerWindow = 3, RequestWindowMinutes = 10, MaxAttempts = 5, ExpirationMinutes = 10, ResetTokenExpirationMinutes = 5 }),
            NullLogger<VerificationCodeService>.Instance, TimeProvider.System);
    }

    private RequestPhoneVerificationService RequestStep() =>
        new(_identity, _codes, _sms, NullLogger<RequestPhoneVerificationService>.Instance);

    private CheckPhoneVerificationService CheckStep() =>
        new(_identity, _codes, _sms, NullLogger<CheckPhoneVerificationService>.Instance);

    private RequestPhoneChangeService IdentityRequest() =>
        new(_identity, _codes, _sms, NullLogger<RequestPhoneChangeService>.Instance);

    private VerifyPhoneChangeIdentityService IdentityVerify() => new(_identity, _codes, _sms);

    // Runs the SMS identity steps on the current phone and returns the single-use token.
    private async Task<string> ProveIdentityAsync()
    {
        await IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, CurrentPhone), "127.0.0.1", CancellationToken.None);
        var response = await IdentityVerify().ExecuteAsync(new VerifyPhoneChangeIdentityDto(Email, CurrentPhone, "123456"), CancellationToken.None);
        return response.ResetToken;
    }
    [Fact]
    public async Task Valid_request_asks_twilio_to_send_the_sms_to_exactly_the_new_phone()
    {
        var token = await ProveIdentityAsync();
        var emailsBefore = _email.Sent.Count;

        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);

        Assert.Equal(new[] { CurrentPhone, NewPhone }, _sms.StartedFor);
        Assert.Empty(_email.Sent); // the whole flow is SMS-based: no SMTP involvement
        Assert.Null(_identity.LastPhoneUpdate); // requesting a code never changes the phone
    }

    [Theory]
    [InlineData("3001234567")]
    [InlineData("+0123456789")]
    [InlineData("+57abc")]
    [InlineData("")]
    public async Task Invalid_phone_never_reaches_twilio_and_keeps_the_identity_token_usable(string phone)
    {
        var token = await ProveIdentityAsync();

        await Assert.ThrowsAsync<InvalidContactFormatException>(() =>
            RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, phone), "127.0.0.1", CancellationToken.None));

        Assert.Equal(new[] { CurrentPhone }, _sms.StartedFor);

        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);
        Assert.Equal(2, _sms.StartedFor.Count);
    }

    [Fact]
    public async Task Phone_is_normalized_but_never_replaced()
    {
        var token = await ProveIdentityAsync();

        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, " +57 300-123-4567 "), "127.0.0.1", CancellationToken.None);

        Assert.Equal(NewPhone, _sms.StartedFor.Last());
    }

    [Fact]
    public async Task Request_requires_the_identity_token()
    {
        await Assert.ThrowsAsync<InvalidResetTokenException>(() =>
            RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, "bogus", NewPhone), "127.0.0.1", CancellationToken.None));

        Assert.Empty(_sms.StartedFor);
    }

    [Fact]
    public async Task Approved_code_updates_the_phone_to_the_verified_number()
    {
        var token = await ProveIdentityAsync();
        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);

        await CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "123456"), CancellationToken.None);

        Assert.Equal((NewPhone, "123456"), _sms.Checked.Last());
        Assert.Equal((_profileId, NewPhone), _identity.LastPhoneUpdate);
    }

    [Fact]
    public async Task Rejected_code_does_not_update_the_phone()
    {
        var token = await ProveIdentityAsync();
        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);
        _sms.NextResult = SmsCheckResult.Rejected;

        await Assert.ThrowsAsync<InvalidCodeException>(() =>
            CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "000000"), CancellationToken.None));

        Assert.Null(_identity.LastPhoneUpdate);
    }

    [Fact]
    public async Task Repeated_rejections_lock_the_challenge_even_if_twilio_would_keep_answering()
    {
        var token = await ProveIdentityAsync();
        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);
        _sms.NextResult = SmsCheckResult.Rejected;

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<InvalidCodeException>(() =>
                CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "000000"), CancellationToken.None));
        }

        await Assert.ThrowsAsync<TooManyAttemptsException>(() =>
            CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "000000"), CancellationToken.None));

        _sms.NextResult = SmsCheckResult.Approved;
        await Assert.ThrowsAsync<TooManyAttemptsException>(() =>
            CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "123456"), CancellationToken.None));
        Assert.Null(_identity.LastPhoneUpdate);
    }

    [Fact]
    public async Task Checking_a_different_phone_than_the_one_verified_is_rejected_without_calling_twilio()
    {
        var token = await ProveIdentityAsync();
        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCodeException>(() =>
            CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, "+573009999999", "123456"), CancellationToken.None));

        Assert.Single(_sms.Checked); // only the identity check, nothing for the mismatching phone
        Assert.Null(_identity.LastPhoneUpdate);
    }

    [Fact]
    public async Task Check_without_a_started_verification_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidCodeException>(() =>
            CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "123456"), CancellationToken.None));

        Assert.Empty(_sms.Checked);
    }

    [Fact]
    public async Task Twilio_failure_is_a_controlled_error_and_the_identity_token_can_be_retried()
    {
        var token = await ProveIdentityAsync();
        _sms.ThrowOnStart = new NotificationDeliveryException(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<NotificationDeliveryException>(() =>
            RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None));

        _sms.ThrowOnStart = null;
        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);
        Assert.Equal(new[] { CurrentPhone, NewPhone }, _sms.StartedFor);
    }

    [Fact]
    public async Task Phone_is_not_marked_updated_when_iam_fails()
    {
        var token = await ProveIdentityAsync();
        await RequestStep().ExecuteAsync(new RequestPhoneVerificationDto(Email, token, NewPhone), "127.0.0.1", CancellationToken.None);
        _identity.ThrowOnNextUpdate = true;

        await Assert.ThrowsAsync<UpstreamUpdateException>(() =>
            CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "123456"), CancellationToken.None));

        // Retry is allowed because the challenge was not consumed.
        _identity.ThrowOnNextUpdate = false;
        await CheckStep().ExecuteAsync(new CheckPhoneVerificationDto(Email, NewPhone, "123456"), CancellationToken.None);
        Assert.Equal((_profileId, NewPhone), _identity.LastPhoneUpdate);
    }

    [Fact]
    public async Task Rate_limits_identity_sms_per_profile()
    {
        for (var i = 0; i < 3; i++)
        {
            await IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, CurrentPhone), "127.0.0.1", CancellationToken.None);
        }

        await Assert.ThrowsAsync<TooManyRequestsException>(() =>
            IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, CurrentPhone), "127.0.0.1", CancellationToken.None));

        Assert.Equal(3, _sms.StartedFor.Count);
    }

    [Fact]
    public async Task Identity_sms_goes_only_to_the_current_phone_when_it_matches_the_stored_one()
    {
        await IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, CurrentPhone), "127.0.0.1", CancellationToken.None);

        Assert.Equal(new[] { CurrentPhone }, _sms.StartedFor);
    }

    [Fact]
    public async Task Identity_sms_is_not_sent_when_the_phone_does_not_match_the_profile()
    {
        await Assert.ThrowsAsync<PhoneMismatchException>(() =>
            IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, "+573009999999"), "127.0.0.1", CancellationToken.None));
        Assert.Empty(_sms.StartedFor);
    }

    [Fact]
    public async Task Identity_sms_is_not_sent_for_an_unknown_email()
    {
        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto("nobody@example.com", CurrentPhone), "127.0.0.1", CancellationToken.None));
        Assert.Empty(_sms.StartedFor);
    }

    [Fact]
    public async Task Invalid_current_phone_never_reaches_twilio()
    {
        await Assert.ThrowsAsync<InvalidContactFormatException>(() =>
            IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, "3119998877"), "127.0.0.1", CancellationToken.None));

        Assert.Empty(_sms.StartedFor);
    }

    [Fact]
    public async Task Rejected_identity_code_yields_no_token()
    {
        await IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, CurrentPhone), "127.0.0.1", CancellationToken.None);
        _sms.NextResult = SmsCheckResult.Rejected;

        await Assert.ThrowsAsync<InvalidCodeException>(() =>
            IdentityVerify().ExecuteAsync(new VerifyPhoneChangeIdentityDto(Email, CurrentPhone, "000000"), CancellationToken.None));
    }

    [Fact]
    public async Task Identity_code_is_checked_against_the_phone_the_sms_was_sent_to()
    {
        await IdentityRequest().ExecuteAsync(new RequestPhoneChangeDto(Email, CurrentPhone), "127.0.0.1", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCodeException>(() =>
            IdentityVerify().ExecuteAsync(new VerifyPhoneChangeIdentityDto(Email, "+573009999999", "123456"), CancellationToken.None));

        Assert.Empty(_sms.Checked);
    }
    [Theory]
    [InlineData(60200, 400, "start", typeof(InvalidContactFormatException))]
    [InlineData(60203, 429, "start", typeof(TooManyRequestsException))]
    [InlineData(60202, 429, "check", typeof(TooManyAttemptsException))]
    [InlineData(20404, 404, "check", typeof(CodeExpiredException))]
    [InlineData(20003, 401, "start", typeof(NotificationDeliveryException))]
    [InlineData(20404, 404, "start", typeof(NotificationDeliveryException))]
    public void Twilio_errors_map_to_controlled_exceptions(int code, int status, string operation, Type expected)
    {
        Assert.IsType(expected, TwilioErrorMapper.Map(code, status, operation));
    }
}