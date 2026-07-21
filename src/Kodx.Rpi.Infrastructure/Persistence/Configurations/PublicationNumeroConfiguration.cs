using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kodx.Rpi.Infrastructure.Persistence.Configurations;

public sealed class PublicationNumeroConfiguration : IEntityTypeConfiguration<PublicationNumero>
{
    public void Configure(EntityTypeBuilder<PublicationNumero> builder)
    {
        builder.ToTable("publication_numeros");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Numero).IsRequired();

        builder.HasOne<Publication>()
            .WithMany()
            .HasForeignKey(n => n.PublicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.Numero);
    }
}
