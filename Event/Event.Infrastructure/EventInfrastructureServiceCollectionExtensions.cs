using Event.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Infrastructure;

public static class EventInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddEventInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EventDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IEventRepository, EventRepository>();

        return services;
    }
}
