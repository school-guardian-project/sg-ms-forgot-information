using ms_forgot_information.Api.EmailChange.Application.UseCase;
using ms_forgot_information.Api.EmailChange.Domain.Ports.In;
using ms_forgot_information.Api.Password.Application.UseCase;
using ms_forgot_information.Api.Password.Domain.Ports.In;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Application.Otp;
using ms_forgot_information.Api.Shared.Application.Services;
using ms_forgot_information.Api.Shared.Domain.Port.Out;
using ms_forgot_information.Api.Shared.Infrastructure.Clients;
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

        services.AddScoped<IForgotPasswordUseCase, ForgotPasswordService>();
        services.AddScoped<IVerifyPasswordResetCodeUseCase, VerifyPasswordResetCodeService>();
        services.AddScoped<IResetPasswordUseCase, ResetPasswordService>();

        services.AddScoped<IRequestEmailChangeUseCase, RequestEmailChangeService>();
        services.AddScoped<IVerifyEmailChangeCodeUseCase, VerifyEmailChangeCodeService>();
        services.AddScoped<ISubmitNewEmailUseCase, SubmitNewEmailService>();
        services.AddScoped<IConfirmEmailChangeUseCase, ConfirmEmailChangeService>();

        return services;
    }
}
