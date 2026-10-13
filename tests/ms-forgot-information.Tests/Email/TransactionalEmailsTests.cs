using NUnit.Framework;
using ms_forgot_information.Api.Shared.Application.Email;
using ms_forgot_information.Api.Shared.Domain.Model;

namespace ms_forgot_information.Tests.Email;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class TransactionalEmailsTests
{
    private static readonly EmailBrandingOptions Branding = new();

    [TestCase(Purpose.PasswordReset)]
    [TestCase(Purpose.EmailChange)]
    [TestCase(Purpose.EmailChangeConfirm)]
    [TestCase(Purpose.PhoneChange)]
    public void EveryPurposeRendersCodeInHtmlAndText(Purpose purpose)
    {
        var email = TransactionalEmails.VerificationCode(Branding, purpose, "482917", 10);

        Assert.That(email.Html, Does.Contain("482917"));
        Assert.That(email.Text, Does.Contain("482917"));
        Assert.That(email.Text, Does.Contain("10 minutos"));
        Assert.That(email.Subject, Does.Contain("Guardian Escolar"));
        Assert.That(email.Html, Does.Not.Contain("<script").IgnoreCase);
        Assert.That(email.Html, Does.Contain("sch00l.guard4n@gmail.com"));
    }

    [Test]
    public void EmailAlwaysDeclaresLightColorScheme()
    {
        var html = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "123456", 10).Html;

        Assert.That(html, Does.Contain("<meta name=\"color-scheme\" content=\"light only\">"));
        Assert.That(html, Does.Contain("supported-color-schemes"));
        Assert.That(html, Does.Contain("bgcolor=\"#F1F5F9\""));
        Assert.That(html, Does.Contain("bgcolor=\"#FFFFFF\""));
    }

    [Test]
    public void DynamicValuesAreHtmlEncoded()
    {
        var email = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "1", 10, "<b>Eve</b>");

        Assert.That(email.Html, Does.Not.Contain("<b>Eve</b>"));
        Assert.That(email.Html, Does.Contain("&lt;b&gt;Eve&lt;/b&gt;"));
    }

    [Test]
    public void OptionalLinksAndImagesOnlyRenderWhenConfigured()
    {
        var bare = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "123456", 10).Html;
        Assert.That(bare, Does.Not.Contain("<img"));
        Assert.That(bare, Does.Not.Contain("Términos de servicio"));

        var branded = TransactionalEmails.VerificationCode(
            new EmailBrandingOptions { LogoUrl = "https://cdn.example/logo.png", TermsUrl = "https://example/terms" },
            Purpose.PasswordReset, "123456", 10).Html;
        Assert.That(branded, Does.Contain("https://cdn.example/logo.png"));
        Assert.That(branded, Does.Contain("Términos de servicio"));
    }

    [Test]
    public void PlainTextContainsOnlyOneSixDigitNumber()
    {
        var text = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "654321", 10).Text;
        Assert.That(System.Text.RegularExpressions.Regex.Matches(text, @"\d{6}"), Has.Exactly(1).Items);
    }
}
