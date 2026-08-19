using Agenda.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Agenda.Application;

public static class AgendaApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAgendaApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AddEventToAgendaUseCase>();
        services.AddScoped<GetAgendaUseCase>();

        return services;
    }
}
