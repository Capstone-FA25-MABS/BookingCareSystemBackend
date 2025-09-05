using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using BookingCare.Shared.EventBus.Abstractions;

namespace BookingCare.Shared.EventBus.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the event bus with event subscriptions
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configureSubscriptions">Action to configure event subscriptions</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseEventBus(this IApplicationBuilder app, Action<IEventBus> configureSubscriptions)
    {
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        configureSubscriptions(eventBus);

        eventBus.StartConsuming();

        return app;
    }

    /// <summary>
    /// Configures common event subscriptions for the BookingCare system
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseBookingCareEventBus(this IApplicationBuilder app)
    {
        return app.UseEventBus(eventBus =>
        {
            // Configure your event subscriptions here based on the service
            // This is a placeholder - each service should configure its own subscriptions
        });
    }
}
