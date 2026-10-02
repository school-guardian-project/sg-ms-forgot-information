namespace ms_forgot_information.Api.Shared.Application.Options;

public sealed class IdentityDirectoryOptions
{
    public const string SectionName = "IdentityDirectory";

    public string IamServiceBaseUrl { get; set; } = string.Empty;
    public string UserManagementServiceBaseUrl { get; set; } = string.Empty;
}

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public string Provider { get; set; } = "Twilio";
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}
