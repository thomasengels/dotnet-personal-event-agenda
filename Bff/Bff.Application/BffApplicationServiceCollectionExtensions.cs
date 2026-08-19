using Bff.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Bff.Application;

public static class BffApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBffApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<GetUserAgendaUseCase>();

        return services;
    }
}
