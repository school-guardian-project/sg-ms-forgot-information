using Microsoft.EntityFrameworkCore;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Configuration;
using ms_forgot_information.Api.Shared.Infrastructure.Persistence.Entity;

namespace ms_forgot_information.Api.Shared.Infrastructure.Persistence.Context;

public class ForgotInformationContext : DbContext
{
    public ForgotInformationContext(DbContextOptions<ForgotInformationContext> options) : base(options) { }

    public DbSet<VerificationRequestEntity> VerificationRequests => Set<VerificationRequestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ForgotInformation");

        modelBuilder.Entity<VerificationRequestEntity>().ToTable("VerificationRequest", schema: "ForgotInformation");

        modelBuilder.ApplyConfiguration(new VerificationRequestConfiguration());
    }
}
