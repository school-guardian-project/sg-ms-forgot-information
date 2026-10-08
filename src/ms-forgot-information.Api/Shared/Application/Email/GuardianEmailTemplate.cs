using System.Net;
using System.Text;

namespace ms_forgot_information.Api.Shared.Application.Email;

public enum EmailTone { Info, Success, Warning, Error }

/// <summary>
/// Reusable building blocks (header, hero, content, CTA, code, alert, support, footer) for every Guardian Escolar email.
/// Table-based layout with inline styles so it renders in Gmail, Outlook, Apple Mail and mobile clients; no JavaScript.
/// The email always declares a light colour scheme and uses bgcolor attributes so clients keep the light palette.
/// All dynamic values are HTML-encoded.
/// </summary>
public static class GuardianEmailTemplate
{
    private const string Font = "Inter,system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif";
    private const string Brand700 = "#1D5BD8";
    private const string Brand800 = "#1E40AF";
    private const string Brand900 = "#172554";

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static (string Bg, string Border, string Text) Palette(EmailTone tone) => tone switch
    {
        EmailTone.Success => ("#ECFDF5", "#A7F3D0", "#065F46"),
        EmailTone.Warning => ("#FEF3C7", "#FBBF24", "#92400E"),
        EmailTone.Error => ("#FEF2F2", "#FECACA", "#B91C1C"),
        _ => ("#EFF6FF", "#BFDBFE", "#1E40AF"),
    };

    public static string Layout(EmailBrandingOptions b, string preheader, string heroTitle, string heroSubtitle, string contentHtml)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"es\" xmlns=\"http://www.w3.org/1999/xhtml\" style=\"color-scheme:light only;\"><head>")
          .Append("<meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
          .Append("<meta name=\"x-apple-disable-message-reformatting\"><meta name=\"color-scheme\" content=\"light only\"><meta name=\"supported-color-schemes\" content=\"light only\">")
          .Append($"<title>{E(heroTitle)} — {E(b.ProductName)}</title>")
          .Append("<style>:root{color-scheme:light only;supported-color-schemes:light only}body,table,td{color-scheme:light only}")
          .Append("@media (prefers-color-scheme:dark){body,.ge-bg{background-color:#F1F5F9!important}.ge-card{background-color:#FFFFFF!important}}")
          .Append("[data-ogsc] .ge-bg,[data-ogsb] .ge-bg{background-color:#F1F5F9!important}")
          .Append("@media only screen and (max-width:620px){.ge-card{width:100%!important;border-radius:0!important}.ge-pad{padding:24px 20px!important}.ge-code{font-size:28px!important;letter-spacing:6px!important}.ge-h1{font-size:24px!important}}</style>")
          .Append("</head>")
          .Append($"<body bgcolor=\"#F1F5F9\" class=\"ge-bg\" style=\"margin:0;padding:0;background-color:#F1F5F9;font-family:{Font};\">")
          .Append($"<div style=\"display:none;max-height:0;overflow:hidden;opacity:0;color:#F1F5F9;\">{E(preheader)}</div>")
          .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#F1F5F9\" class=\"ge-bg\" style=\"background-color:#F1F5F9;\"><tr><td align=\"center\" bgcolor=\"#F1F5F9\" class=\"ge-bg\" style=\"padding:24px 12px;background-color:#F1F5F9;\">")
          .Append(Header(b))
          .Append("<table role=\"presentation\" class=\"ge-card\" width=\"640\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#FFFFFF\" style=\"width:640px;max-width:640px;background-color:#FFFFFF;border:1px solid #E2E8F0;border-radius:20px;overflow:hidden;box-shadow:0 8px 24px rgba(15,23,42,0.08);\">")
          .Append(Hero(b, heroTitle, heroSubtitle))
          .Append($"<tr><td class=\"ge-pad\" bgcolor=\"#FFFFFF\" style=\"padding:32px 40px 8px 40px;background-color:#FFFFFF;font-family:{Font};color:#475569;font-size:16px;line-height:1.6;\">{contentHtml}</td></tr>")
          .Append(Support(b))
          .Append("</table>")
          .Append(Footer(b))
          .Append("</td></tr></table></body></html>");
        return sb.ToString();
    }

    public static string Header(EmailBrandingOptions b)
    {
        var logo = string.IsNullOrWhiteSpace(b.LogoUrl)
            ? string.Empty
            : $"<img src=\"{E(b.LogoUrl)}\" alt=\"\" height=\"36\" style=\"height:36px;vertical-align:middle;border:0;margin-right:10px;\">";
        return "<table role=\"presentation\" width=\"640\" class=\"ge-card\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:640px;max-width:100%;\"><tr>" +
               $"<td align=\"center\" style=\"padding:0 0 16px 0;font-family:{Font};font-size:20px;font-weight:700;color:{Brand900};\">{logo}{E(b.ProductName)}</td></tr></table>";
    }

    public static string Hero(EmailBrandingOptions b, string title, string subtitle)
    {
        var image = string.IsNullOrWhiteSpace(b.HeroImageUrl)
            ? string.Empty
            : $"<tr><td align=\"center\" style=\"padding-top:20px;\"><img src=\"{E(b.HeroImageUrl)}\" alt=\"Bus escolar, ubicación y escudo de protección\" width=\"260\" style=\"width:260px;max-width:70%;height:auto;border:0;\"></td></tr>";
        return $"<tr><td bgcolor=\"{Brand700}\" style=\"background-color:{Brand700};background-image:linear-gradient(135deg,{Brand800} 0%,{Brand700} 60%,#3B82F6 100%);padding:36px 40px;text-align:center;\">" +
               "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">" +
               $"<tr><td align=\"center\"><span style=\"display:inline-block;background-color:#FBBF24;color:{Brand900};font-family:{Font};font-size:12px;font-weight:700;letter-spacing:1px;padding:5px 14px;border-radius:999px;\">{E(b.Tagline).ToUpperInvariant()}</span></td></tr>" +
               $"<tr><td align=\"center\" class=\"ge-h1\" style=\"padding-top:16px;font-family:{Font};font-size:28px;line-height:1.25;font-weight:700;color:#FFFFFF;\">{E(title)}</td></tr>" +
               $"<tr><td align=\"center\" style=\"padding-top:10px;font-family:{Font};font-size:15px;line-height:1.5;color:#DBEAFE;\">{E(subtitle)}</td></tr>" +
               image + "</table></td></tr>";
    }

    public static string Greeting(string? userName) =>
        $"<p style=\"margin:0 0 16px 0;font-size:18px;font-weight:600;color:#0F172A;\">{(string.IsNullOrWhiteSpace(userName) ? "Hola," : $"Hola {E(userName)},")}</p>";

    public static string Paragraph(string text) =>
        $"<p style=\"margin:0 0 16px 0;font-size:16px;line-height:1.6;color:#475569;\">{E(text)}</p>";

    public static string SmallText(string text) =>
        $"<p style=\"margin:16px 0 0 0;font-size:13px;line-height:1.5;color:#64748B;\">{E(text)}</p>";

    public static string VerificationCode(string code, string caption) =>
        "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:8px 0 20px 0;\"><tr><td align=\"center\" bgcolor=\"#EFF6FF\" " +
        "style=\"background-color:#EFF6FF;border:2px dashed #93C5FD;border-radius:14px;padding:22px 12px;\">" +
        $"<div style=\"font-family:{Font};font-size:12px;font-weight:700;letter-spacing:1px;color:#64748B;text-transform:uppercase;\">{E(caption)}</div>" +
        $"<div class=\"ge-code\" style=\"font-family:'SFMono-Regular',Consolas,'Courier New',monospace;font-size:36px;font-weight:700;letter-spacing:10px;color:{Brand900};padding-top:8px;\">{E(code)}</div>" +
        "</td></tr></table>";

    /// <summary>Pill CTA (bulletproof: a bgcolor table cell wrapping the link, so Outlook keeps the colour).</summary>
    public static string Cta(string text, string url) =>
        "<table role=\"presentation\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:8px auto 20px auto;\"><tr>" +
        $"<td align=\"center\" bgcolor=\"{Brand800}\" style=\"background-color:{Brand800};border-radius:999px;\">" +
        $"<a href=\"{E(url)}\" target=\"_blank\" style=\"display:inline-block;padding:14px 32px;font-family:{Font};font-size:16px;font-weight:600;color:#FFFFFF;text-decoration:none;border-radius:999px;\">{E(text)}</a>" +
        "</td></tr></table>";

    public static string Alert(EmailTone tone, string title, string message)
    {
        var (bg, border, text) = Palette(tone);
        return $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:8px 0 8px 0;\"><tr><td bgcolor=\"{bg}\" style=\"background-color:{bg};border:1px solid {border};border-left:4px solid {text};border-radius:10px;padding:14px 16px;font-family:{Font};font-size:14px;line-height:1.5;color:{text};\">" +
               $"<strong>{E(title)}</strong><br>{E(message)}</td></tr></table>";
    }

    public static string Support(EmailBrandingOptions b) =>
        $"<tr><td class=\"ge-pad\" bgcolor=\"#FFFFFF\" style=\"padding:8px 40px 32px 40px;background-color:#FFFFFF;font-family:{Font};font-size:14px;line-height:1.6;color:#64748B;\">" +
        "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr><td style=\"border-top:1px solid #E2E8F0;padding-top:20px;\">" +
        "<strong style=\"color:#1E293B;\">¿Necesitas ayuda?</strong><br>" +
        $"<a href=\"mailto:{E(b.SupportEmail)}\" style=\"color:{Brand700};text-decoration:underline;\">{E(b.SupportEmail)}</a> · {E(b.SupportPhone)}" +
        "</td></tr></table></td></tr>";

    public static string Footer(EmailBrandingOptions b)
    {
        var links = new List<string>();
        if (!string.IsNullOrWhiteSpace(b.TermsUrl)) links.Add($"<a href=\"{E(b.TermsUrl)}\" style=\"color:#64748B;text-decoration:underline;\">Términos de servicio</a>");
        if (!string.IsNullOrWhiteSpace(b.PrivacyUrl)) links.Add($"<a href=\"{E(b.PrivacyUrl)}\" style=\"color:#64748B;text-decoration:underline;\">Política de privacidad</a>");
        var legal = links.Count == 0 ? string.Empty : $"<br>{string.Join(" · ", links)}";
        return "<table role=\"presentation\" class=\"ge-card\" width=\"640\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:640px;max-width:100%;\"><tr>" +
               $"<td align=\"center\" style=\"padding:20px 16px 0 16px;font-family:{Font};font-size:12px;line-height:1.6;color:#94A3B8;\">" +
               $"<strong style=\"color:#64748B;\">{E(b.ProductName)}</strong> · {E(b.Tagline)}<br>© {DateTime.UtcNow.Year} {E(b.ProductName)}. Todos los derechos reservados.{legal}" +
               "</td></tr></table>";
    }
}