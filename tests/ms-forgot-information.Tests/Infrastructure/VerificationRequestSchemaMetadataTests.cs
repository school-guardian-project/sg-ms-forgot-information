using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Context;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Entity;
using NUnit.Framework;

namespace ms_forgot_information.Tests.Infrastructure;

[TestFixture]
public sealed class VerificationRequestSchemaMetadataTests
{
    [Test]
    public void EfMetadataMatchesTheSqlCharacterSetDefaultsAndStatusConstraint()
    {
        var options = new DbContextOptionsBuilder<ForgotInformationContext>()
            .UseSqlServer("Server=localhost;Database=metadata-only;User Id=unused;Password=unused;TrustServerCertificate=True")
            .Options;

        using var context = new ForgotInformationContext(options);
        var request = context.Model.FindEntityType(typeof(VerificationRequestEntity))!;
        var createScript = context.Database.GenerateCreateScript();

        Assert.Multiple(() =>
        {
            AssertAnsiString(request, nameof(VerificationRequestEntity.Purpose), 20);
            AssertAnsiString(request, nameof(VerificationRequestEntity.Target), 100);
            AssertAnsiString(request, nameof(VerificationRequestEntity.CodeHash), 255);
            AssertAnsiString(request, nameof(VerificationRequestEntity.ResetTokenHash), 255);
            AssertAnsiString(request, nameof(VerificationRequestEntity.Status), 20);
            AssertAnsiString(request, nameof(VerificationRequestEntity.RequestIp), 50);
            Assert.That(request.FindProperty(nameof(VerificationRequestEntity.AttemptCount))!.GetDefaultValue(), Is.EqualTo((byte)0));
            Assert.That(request.FindProperty(nameof(VerificationRequestEntity.MaxAttempts))!.GetDefaultValue(), Is.EqualTo((byte)5));
            Assert.That(request.FindProperty(nameof(VerificationRequestEntity.Status))!.GetDefaultValue(), Is.EqualTo(VerificationStatus.Pending));
            Assert.That(request.FindProperty(nameof(VerificationRequestEntity.CreatedAt))!.GetDefaultValueSql(), Is.EqualTo("GETUTCDATE()"));
            Assert.That(createScript, Does.Contain("[Status] IN ('Pending', 'Verified', 'Consumed', 'Expired', 'Locked')"));
        });
    }

    [Test]
    public void RuntimeInitializerUsesSqlDefaultsWithoutApplyingTheStalePurposeCheck()
    {
        var field = typeof(VerificationRequestSchemaInitializer)
            .GetField("Sql", BindingFlags.NonPublic | BindingFlags.Static)!;
        var sql = (string)field.GetRawConstantValue()!;

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("[Purpose] varchar(20) NOT NULL"));
            Assert.That(sql, Does.Contain("[Target] varchar(100) NOT NULL"));
            Assert.That(sql, Does.Contain("[CodeHash] varchar(255) NOT NULL"));
            Assert.That(sql, Does.Contain("[RequestIp] varchar(50) NOT NULL"));
            Assert.That(sql, Does.Contain("[AttemptCount] tinyint NOT NULL DEFAULT 0"));
            Assert.That(sql, Does.Contain("[MaxAttempts] tinyint NOT NULL DEFAULT 5"));
            Assert.That(sql, Does.Contain("DEFAULT 'Pending'"));
            Assert.That(sql, Does.Contain("DEFAULT GETUTCDATE()"));
            Assert.That(Regex.IsMatch(sql, @"\[Purpose\][^\r\n,]*CHECK"), Is.False);
        });
    }

    private static void AssertAnsiString(Microsoft.EntityFrameworkCore.Metadata.IEntityType entity, string propertyName, int length)
    {
        var property = entity.FindProperty(propertyName)!;
        Assert.That(property.GetMaxLength(), Is.EqualTo(length), propertyName);
        Assert.That(property.IsUnicode(), Is.False, propertyName);
    }
}
