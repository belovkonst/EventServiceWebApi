using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Application.Services;

namespace EventServiceWebApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IEventService, EventService>();

        return services;
    }
}
