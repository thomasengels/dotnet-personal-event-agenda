using Agenda.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Agenda.Infrastructure;

public static class AgendaInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAgendaInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AgendaDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAgendaRepository, AgendaRepository>();

        return services;
    }
}
