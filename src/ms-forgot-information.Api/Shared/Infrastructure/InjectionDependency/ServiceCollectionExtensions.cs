using ms_forgot_information.Api.Email.Application.UseCase;
using ms_forgot_information.Api.Email.Domain.Ports.In;
using ms_forgot_information.Api.Password.Application.UseCase;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Phone.Application.UseCase;
using ms_forgot_information.Api.Phone.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Infrastructure.Clients;
using ms_forgot_information.Api.Shared.Infrastructure.Events;
using ms_forgot_information.Api.Shared.Infrastructure.Notifications;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Repository;

namespace ms_forgot_information.Api.Shared.Infrastructure.InjectionDependency;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISecretHasher>(_ =>
        {
            var pepper = configuration.GetSection(OtpOptions.SectionName)[nameof(OtpOptions.HashPepper)];
            if (string.IsNullOrWhiteSpace(pepper))
            {
                throw new InvalidOperationException("Otp:HashPepper must be configured.");
            }
            return new SecretHasher(pepper);
        });

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IVerificationRequestRepository, VerificationRequestRepository>();
        services.AddScoped<VerificationCodeService>();

        services.AddScoped<IIdentityDirectoryClient, IdentityDirectoryHttpClient>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Sms:Provider=Log writes the message to the application log instead of calling Twilio —
        // used for local testing without a real Twilio account (see TESTING.md).
        var smsProvider = configuration.GetSection(SmsOptions.SectionName)[nameof(SmsOptions.Provider)];
        if (string.Equals(smsProvider, "Log", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<ISmsSender, LoggingSmsSender>();
        }
        else
        {
            services.AddScoped<ISmsSender, TwilioSmsSender>();
        }

        services.AddSingleton<IDomainEventPublisher, LoggingDomainEventPublisher>();

        services.AddScoped<IForgotPasswordUseCase, ForgotPasswordService>();
        services.AddScoped<IVerifyPasswordResetCodeUseCase, VerifyPasswordResetCodeService>();
        services.AddScoped<IResetPasswordUseCase, ResetPasswordService>();
        services.AddScoped<IRequestPasswordChangeCodeUseCase, RequestPasswordChangeCodeService>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordService>();

        services.AddScoped<IRequestEmailChangeUseCase, RequestEmailChangeService>();
        services.AddScoped<IConfirmEmailChangeUseCase, ConfirmEmailChangeService>();

        services.AddScoped<IRequestPhoneChangeUseCase, RequestPhoneChangeService>();
        services.AddScoped<IConfirmPhoneChangeUseCase, ConfirmPhoneChangeService>();

        return services;
    }
}
