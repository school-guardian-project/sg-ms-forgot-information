using Xunit;
using ms_forgot_information.Api.Shared.Application.Email;
using ms_forgot_information.Api.Shared.Domain.Model;

namespace ms_forgot_information.Tests.Email;

public class TransactionalEmailsTests
{
    private static readonly EmailBrandingOptions Branding = new();

    [Theory]
    [InlineData(Purpose.PasswordReset)]
    [InlineData(Purpose.EmailChange)]
    [InlineData(Purpose.EmailChangeConfirm)]
    [InlineData(Purpose.PhoneChange)]
    public void EveryPurposeRendersCodeInHtmlAndText(Purpose purpose)
    {
        var email = TransactionalEmails.VerificationCode(Branding, purpose, "482917", 10);

        Assert.Contains("482917", email.Html);
        Assert.Contains("482917", email.Text);
        Assert.Contains("10 minutos", email.Text);
        Assert.Contains("Guardian Escolar", email.Subject);
        Assert.DoesNotContain("<script", email.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sch00l.guard4n@gmail.com", email.Html);
    }

    [Fact]
    public void EmailAlwaysDeclaresLightColorScheme()
    {
        var html = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "123456", 10).Html;

        Assert.Contains("<meta name=\"color-scheme\" content=\"light only\">", html);
        Assert.Contains("supported-color-schemes", html);
        Assert.Contains("bgcolor=\"#F1F5F9\"", html);
        Assert.Contains("bgcolor=\"#FFFFFF\"", html);
    }

    [Fact]
    public void DynamicValuesAreHtmlEncoded()
    {
        var email = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "1", 10, "<b>Eve</b>");

        Assert.DoesNotContain("<b>Eve</b>", email.Html);
        Assert.Contains("&lt;b&gt;Eve&lt;/b&gt;", email.Html);
    }

    [Fact]
    public void OptionalLinksAndImagesOnlyRenderWhenConfigured()
    {
        var bare = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "123456", 10).Html;
        Assert.DoesNotContain("<img", bare);
        Assert.DoesNotContain("Términos de servicio", bare);

        var branded = TransactionalEmails.VerificationCode(
            new EmailBrandingOptions { LogoUrl = "https://cdn.example/logo.png", TermsUrl = "https://example/terms" },
            Purpose.PasswordReset, "123456", 10).Html;
        Assert.Contains("https://cdn.example/logo.png", branded);
        Assert.Contains("Términos de servicio", branded);
    }

    [Fact]
    public void PlainTextContainsOnlyOneSixDigitNumber()
    {
        var text = TransactionalEmails.VerificationCode(Branding, Purpose.PasswordReset, "654321", 10).Text;
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(text, @"\d{6}"));
    }
}