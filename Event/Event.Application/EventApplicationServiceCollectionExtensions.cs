using Event.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Application;

public static class EventApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddEventApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateEventUseCase>();
        services.AddScoped<GetEventByIdUseCase>();

        return services;
    }
}
