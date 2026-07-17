using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kodx.Rpi.Infrastructure.Persistence.Configurations;

public sealed class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("publications");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Numero).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.OwnsOne(p => p.Payload, payload => payload.ToJson());

        builder.HasOne<RpiEdition>()
            .WithMany()
            .HasForeignKey(p => p.RpiEditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.RpiEditionId);
        builder.HasIndex(p => p.Numero);
    }
}
