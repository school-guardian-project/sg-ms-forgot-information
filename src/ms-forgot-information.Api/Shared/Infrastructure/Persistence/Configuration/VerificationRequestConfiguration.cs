using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Entity;

namespace ms_forgot_information.Api.Shared.Infrastructure.Persistence.Configuration;

public class VerificationRequestConfiguration : IEntityTypeConfiguration<VerificationRequestEntity>
{
    public void Configure(EntityTypeBuilder<VerificationRequestEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Target).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CodeHash).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ResetTokenHash).HasMaxLength(255);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RequestIp).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => new { x.ProfileId, x.Purpose, x.Status })
            .HasDatabaseName("idx_verificationrequest_profile_purpose_status");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_verificationrequest_expiresat");
    }
}
