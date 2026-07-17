using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kodx.Rpi.Infrastructure.Persistence.Configurations;

public sealed class RpiEditionConfiguration : IEntityTypeConfiguration<RpiEdition>
{
    public void Configure(EntityTypeBuilder<RpiEdition> builder)
    {
        builder.ToTable("rpi_editions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Edicao).IsRequired();
        builder.Property(e => e.Tipo).IsRequired();
        builder.Property(e => e.DataPublicacao).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.Edicao, e.Tipo }).IsUnique();
    }
}
