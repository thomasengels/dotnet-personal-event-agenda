using Event.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Application;

public static class EventApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddEventApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateEventUseCase>();
        services.AddScoped<GetEventByIdUseCase>();
        services.AddScoped<GetEventsUseCase>();

        return services;
    }
}
