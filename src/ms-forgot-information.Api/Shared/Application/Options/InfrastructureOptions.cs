namespace ms_forgot_information.Api.Shared.Application.Options;

public sealed class IdentityDirectoryOptions
{
    public const string SectionName = "IdentityDirectory";

    public string IamServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>Shared secret sent to iam-service in X-Internal-Api-Key. Must come from a secret store in production.</summary>
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
