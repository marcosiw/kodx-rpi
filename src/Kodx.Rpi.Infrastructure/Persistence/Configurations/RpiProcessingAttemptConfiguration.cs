using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kodx.Rpi.Infrastructure.Persistence.Configurations;

public sealed class RpiProcessingAttemptConfiguration : IEntityTypeConfiguration<RpiProcessingAttempt>
{
    public void Configure(EntityTypeBuilder<RpiProcessingAttempt> builder)
    {
        builder.ToTable("rpi_processing_attempts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Stage).IsRequired();
        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.ErrorMessage);
        builder.Property(a => a.StartedAt).IsRequired();
        builder.Property(a => a.FinishedAt).IsRequired();

        builder.HasOne<RpiEdition>()
            .WithMany()
            .HasForeignKey(a => a.RpiEditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.RpiEditionId, a.Stage });
    }
}
