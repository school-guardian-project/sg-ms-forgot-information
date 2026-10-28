using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ms_forgot_information.Api.Shared.Domain.Model;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Entity;

namespace ms_forgot_information.Api.Shared.Infrastructure.Persistence.Configuration;

public class VerificationRequestConfiguration : IEntityTypeConfiguration<VerificationRequestEntity>
{
    public void Configure(EntityTypeBuilder<VerificationRequestEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.Target).IsRequired().HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.CodeHash).IsRequired().HasMaxLength(255).IsUnicode(false);
        builder.Property(x => x.ResetTokenHash).HasMaxLength(255).IsUnicode(false);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false).HasDefaultValue(VerificationStatus.Pending).IsRequired();
        builder.Property(x => x.AttemptCount).HasDefaultValue((byte)0);
        builder.Property(x => x.MaxAttempts).HasDefaultValue((byte)5);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.RequestIp).IsRequired().HasMaxLength(50).IsUnicode(false);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_VerificationRequest_Status",
            "[Status] IN ('Pending', 'Verified', 'Consumed', 'Expired', 'Locked')"));

        builder.HasIndex(x => new { x.ProfileId, x.Purpose, x.Status })
            .HasDatabaseName("idx_verificationrequest_profile_purpose_status");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_verificationrequest_expiresat");
    }
}
