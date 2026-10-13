namespace ms_forgot_information.Api.Shared.Application.Options;

public sealed class IdentityDirectoryOptions
{
    public const string SectionName = "IdentityDirectory";

    /// <summary>Address of the ms-iam internal gRPC server (contract iam.v1).</summary>
    public string GrpcAddress { get; set; } = string.Empty;

    /// <summary>Shared secret sent to iam-service in the x-internal-api-key gRPC metadata. Must come from a secret store in production.</summary>
    public string IamApiKey { get; set; } = string.Empty;
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
    public int TimeoutSeconds { get; set; } = 15;
}

public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";

    public string AccountSid { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string VerifyServiceSid { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret)
        && !string.IsNullOrWhiteSpace(VerifyServiceSid);
}
