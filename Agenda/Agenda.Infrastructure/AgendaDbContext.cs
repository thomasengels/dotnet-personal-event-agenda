using Agenda.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Infrastructure;

public sealed class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options)
{
    public DbSet<AgendaItemEntity> AgendaItems => Set<AgendaItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AgendaItemEntityConfiguration());
    }
}
