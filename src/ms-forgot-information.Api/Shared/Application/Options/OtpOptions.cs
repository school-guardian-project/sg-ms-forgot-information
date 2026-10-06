namespace ms_forgot_information.Api.Shared.Application.Options;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";
    public const int ResetCodeLength = 6;

    public int ExpirationMinutes { get; set; } = 10;
    public byte MaxAttempts { get; set; } = 5;
    public int MaxRequestsPerWindow { get; set; } = 3;
    public int RequestWindowMinutes { get; set; } = 10;
    public int ResetTokenExpirationMinutes { get; set; } = 5;

    /// <summary>Server-side secret mixed into every code hash. Must come from a secret store in production.</summary>
    public string HashPepper { get; set; } = string.Empty;
}
