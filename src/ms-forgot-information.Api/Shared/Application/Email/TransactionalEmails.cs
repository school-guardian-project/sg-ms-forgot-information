using ms_forgot_information.Api.Shared.Domain.Model;

namespace ms_forgot_information.Api.Shared.Application.Email;

public sealed record EmailContent(string Subject, string Text, string Html);

/// <summary>
/// Concrete emails built on <see cref="GuardianEmailTemplate"/>. Every email shares the same visual base;
/// only the copy and the components change.
/// </summary>
public static class TransactionalEmails
{
    private sealed record Copy(string Subject, string Hero, string HeroSubtitle, string Intro, string Caption, string Ignore);

    private static Copy CopyFor(Purpose purpose) => purpose switch
    {
        Purpose.EmailChange => new("Código para cambiar tu correo — Guardian Escolar", "Confirma el cambio de correo",
            "Protegemos tu cuenta en cada cambio", "Recibimos una solicitud para cambiar el correo de tu cuenta de Guardian Escolar. Usa este código para confirmar que eres tú.",
            "Código de verificación", "Si no solicitaste este cambio, puedes ignorar este correo de forma segura. Tu correo actual no cambiará."),
        Purpose.EmailChangeConfirm => new("Verifica tu nuevo correo — Guardian Escolar", "Verifica tu nuevo correo",
            "Un último paso para actualizar tu cuenta", "Usa este código para verificar esta dirección como el nuevo correo de tu cuenta de Guardian Escolar.",
            "Código de verificación", "Si no solicitaste este cambio, puedes ignorar este correo de forma segura."),
        Purpose.PhoneChange => new("Código para cambiar tu teléfono — Guardian Escolar", "Confirma el cambio de teléfono",
            "Protegemos tu cuenta en cada cambio", "Recibimos una solicitud para cambiar el teléfono de tu cuenta de Guardian Escolar. Usa este código para confirmar que eres tú.",
            "Código de verificación", "Si no solicitaste este cambio, puedes ignorar este correo de forma segura. Tu teléfono actual no cambiará."),
        _ => new("Código para recuperar tu contraseña — Guardian Escolar", "Restablecer contraseña",
            "Recupera el acceso a tu cuenta de forma segura", "Recibimos una solicitud para restablecer la contraseña de tu cuenta en Guardian Escolar. Ingresa este código en la aplicación para continuar.",
            "Tu código de recuperación", "Si no solicitaste este cambio, puedes ignorar este correo de forma segura. Tu contraseña actual no cambiará."),
    };

    public static EmailContent VerificationCode(EmailBrandingOptions branding, Purpose purpose, string code, int expirationMinutes, string? userName = null)
    {
        var c = CopyFor(purpose);
        var expiry = $"Vence en {expirationMinutes} minutos y solo se puede usar una vez.";

        var html = GuardianEmailTemplate.Layout(
            branding,
            preheader: $"{c.Caption}: {code}. {expiry}",
            heroTitle: c.Hero,
            heroSubtitle: c.HeroSubtitle,
            contentHtml:
                GuardianEmailTemplate.Greeting(userName) +
                GuardianEmailTemplate.Paragraph(c.Intro) +
                GuardianEmailTemplate.VerificationCode(code, c.Caption) +
                GuardianEmailTemplate.Alert(EmailTone.Warning, "Por tu seguridad", $"{expiry} Nunca compartas este código con nadie; el equipo de {branding.ProductName} no te lo pedirá.") +
                GuardianEmailTemplate.SmallText(c.Ignore));

        var text = $"{c.Intro}\n\n{c.Caption}: {code}\n\n{expiry}\n{c.Ignore}\n\n" +
                   $"{branding.ProductName} · {branding.SupportEmail}";

        return new EmailContent(c.Subject, text, html);
    }
}