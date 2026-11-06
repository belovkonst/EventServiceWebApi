using EventServiceWebApi.Application.BackgroundServices;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Application.Repositories;
using EventServiceWebApi.Application.Services;

namespace EventServiceWebApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IEventService, EventService>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<IBookingRepository, BookingRepository>();

        services.AddHostedService<BookingProcessingService>();

        return services;
    }
}
