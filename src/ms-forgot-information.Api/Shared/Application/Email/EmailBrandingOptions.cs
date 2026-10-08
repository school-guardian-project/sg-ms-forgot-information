namespace ms_forgot_information.Api.Shared.Application.Email;

/// <summary>Branding values shared by every transactional email (section "Branding").</summary>
public sealed class EmailBrandingOptions
{
    public const string SectionName = "Branding";

    public string ProductName { get; set; } = "Guardian Escolar";
    public string Tagline { get; set; } = "Sistema de Monitoreo Escolar";

    /// <summary>Public https URL of a PNG logo. Email clients block inline SVG/data URIs, so it must be hosted. Optional.</summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>Public https URL of the hero illustration (school bus + GPS + shield). Optional.</summary>
    public string HeroImageUrl { get; set; } = string.Empty;

    public string SupportEmail { get; set; } = "sch00l.guard4n@gmail.com";
    public string SupportPhone { get; set; } = "+57 314 4308274";

    /// <summary>Left empty on purpose: links are only rendered when the pages really exist.</summary>
    public string TermsUrl { get; set; } = string.Empty;
    public string PrivacyUrl { get; set; } = string.Empty;
}