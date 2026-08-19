using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Agenda.Infrastructure;

public sealed class AgendaDbContextFactory : IDesignTimeDbContextFactory<AgendaDbContext>
{
    public AgendaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AgendaDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=agenda;Username=agenda;Password=agenda");

        return new AgendaDbContext(optionsBuilder.Options);
    }
}
