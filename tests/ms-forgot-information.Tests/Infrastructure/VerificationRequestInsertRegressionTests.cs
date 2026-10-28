using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Context;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Entity;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Repository;
using NUnit.Framework;

namespace ms_forgot_information.Tests.Infrastructure;

public class VerificationRequestInsertRegressionTests
{
    [Test]
    public async Task AddAsync_SendsInitialAttemptCountWithoutDependingOnDatabaseDefaults()
    {
        var options = new DbContextOptionsBuilder<ForgotInformationContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true")
            .Options;
        using var context = new CapturingContext(options);
        var request = VerificationRequest.Issue(
            Guid.NewGuid(), Purpose.PasswordReset, "student@example.com", "hash",
            TimeSpan.FromMinutes(5), 5, "127.0.0.1", DateTime.UtcNow);

        await new VerificationRequestRepository(context).AddAsync(request, CancellationToken.None);

        var entry = context.ChangeTracker.Entries<VerificationRequestEntity>().Single();
        Assert.That(context.SaveCalled, Is.True);
        Assert.That(entry.Entity.AttemptCount, Is.Zero);
        Assert.That(entry.Entity.MaxAttempts, Is.EqualTo(5));
        Assert.That(entry.Entity.Status, Is.EqualTo(VerificationStatus.Pending));
        foreach (var name in new[]
        {
            nameof(VerificationRequestEntity.AttemptCount),
            nameof(VerificationRequestEntity.MaxAttempts),
            nameof(VerificationRequestEntity.Status),
            nameof(VerificationRequestEntity.CreatedAt)
        })
        {
            var property = entry.Metadata.FindProperty(name)!;
            Assert.That(property.ValueGenerated, Is.EqualTo(ValueGenerated.Never), name);
            Assert.That(property.GetBeforeSaveBehavior(), Is.EqualTo(PropertySaveBehavior.Save), name);
            Assert.That(entry.Property(name).IsTemporary, Is.False, name);
        }
    }

    private sealed class CapturingContext(DbContextOptions<ForgotInformationContext> options)
        : ForgotInformationContext(options)
    {
        public bool SaveCalled { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            return Task.FromResult(1);
        }
    }
}
