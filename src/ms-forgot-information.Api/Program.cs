using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Infrastructure.InjectionDependency;
using ms_forgot_information.Api.Shared.Infrastructure.Middleware;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (builder.Environment.IsDevelopment() && corsOrigins.Length == 0)
{
    corsOrigins = ["http://localhost:4200"];
}

if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
        options.AddPolicy("web-client", policy =>
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()));
}

builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<IdentityDirectoryOptions>(builder.Configuration.GetSection(IdentityDirectoryOptions.SectionName));

builder.Services.AddDbContext<ForgotInformationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var identityDirectoryOptions = builder.Configuration.GetSection(IdentityDirectoryOptions.SectionName).Get<IdentityDirectoryOptions>()
    ?? new IdentityDirectoryOptions();
var otpOptions = builder.Configuration.GetSection(OtpOptions.SectionName).Get<OtpOptions>() ?? new OtpOptions();

if (string.IsNullOrWhiteSpace(otpOptions.HashPepper))
{
    throw new InvalidOperationException("Otp:HashPepper must be configured.");
}

if (!builder.Environment.IsDevelopment())
{
    // Fail fast instead of silently running production with test/empty settings.
    var smtpOptions = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
    if (string.IsNullOrWhiteSpace(smtpOptions.Host) || string.IsNullOrWhiteSpace(smtpOptions.FromAddress))
    {
        throw new InvalidOperationException("Smtp:Host and Smtp:FromAddress must be configured outside Development.");
    }

    if (string.IsNullOrWhiteSpace(identityDirectoryOptions.IamApiKey))
    {
        throw new InvalidOperationException("IdentityDirectory:IamApiKey must be configured outside Development.");
    }
}

builder.Services.AddHttpClient("iam-service", client =>
{
    client.BaseAddress = new Uri(identityDirectoryOptions.IamServiceBaseUrl);

    // Service-to-service credential expected by ms-iam on /api/profiles/**.
    if (!string.IsNullOrWhiteSpace(identityDirectoryOptions.IamApiKey))
    {
        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", identityDirectoryOptions.IamApiKey);
    }
});

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Defense-in-depth per-IP limiter for anonymous OTP endpoints; the authoritative
    // per-profile limiter lives in VerificationCodeService (see DEC-005).
    options.AddPolicy("otp-public", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ForgotInformationContext>();
    await VerificationRequestSchemaInitializer.InitializeAsync(dbContext);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

if (corsOrigins.Length > 0)
{
    app.UseCors("web-client");
}

app.UseRateLimiter();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.Run();

public partial class Program;
