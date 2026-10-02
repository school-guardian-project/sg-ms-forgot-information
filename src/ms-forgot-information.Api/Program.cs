using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ms_forgot_information.Api.Shared.Application.Options;
using ms_forgot_information.Api.Shared.Infrastructure.InjectionDependency;
using ms_forgot_information.Api.Shared.Infrastructure.Middleware;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection(SmsOptions.SectionName));
builder.Services.Configure<IdentityDirectoryOptions>(builder.Configuration.GetSection(IdentityDirectoryOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<ForgotInformationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var identityDirectoryOptions = builder.Configuration.GetSection(IdentityDirectoryOptions.SectionName).Get<IdentityDirectoryOptions>()
    ?? new IdentityDirectoryOptions();

builder.Services.AddHttpClient("iam-service", client =>
{
    client.BaseAddress = new Uri(identityDirectoryOptions.IamServiceBaseUrl);
});

builder.Services.AddHttpClient("user-management-service", client =>
{
    client.BaseAddress = new Uri(identityDirectoryOptions.UserManagementServiceBaseUrl);
});

builder.Services.AddHttpClient("twilio");

builder.Services.AddApplicationServices(builder.Configuration);

// JWT RS256 per ADR-008: this service only validates with the public key, never signs tokens.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = LoadPublicKey(jwtOptions.PublicKey)
    };
});

builder.Services.AddAuthorization();

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

    // Convenience only: creates the schema/table straight from the EF model so the service is
    // testable without running the Liquibase changelogs from sg-db. Production uses Liquibase.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ForgotInformationContext>().Database.EnsureCreatedAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.Run();

static RsaSecurityKey LoadPublicKey(string pem)
{
    if (string.IsNullOrWhiteSpace(pem))
    {
        throw new InvalidOperationException("Jwt:PublicKey must be configured (RS256 public key, see ADR-008).");
    }

    var rsa = RSA.Create();
    rsa.ImportFromPem(pem);
    return new RsaSecurityKey(rsa);
}

public partial class Program;
