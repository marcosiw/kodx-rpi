using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;

namespace Kodx.Rpi.Infrastructure.Persistence;

public sealed class KodxRpiDbContext(DbContextOptions<KodxRpiDbContext> options) : DbContext(options)
{
    public DbSet<RpiEdition> RpiEditions => Set<RpiEdition>();
    public DbSet<RpiProcessingAttempt> RpiProcessingAttempts => Set<RpiProcessingAttempt>();
    public DbSet<Publication> Publications => Set<Publication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KodxRpiDbContext).Assembly);
    }
}
