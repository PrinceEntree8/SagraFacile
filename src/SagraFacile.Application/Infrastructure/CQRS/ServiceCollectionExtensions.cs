using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SagraFacile.Application.Infrastructure.CQRS;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and all command/query handlers found in the provided assemblies.
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IMediator, Mediator>();

        var commandHandlerType = typeof(ICommandHandler<,>);
        var queryHandlerType = typeof(IQueryHandler<,>);

        foreach (var assembly in assemblies)
        {
            var handlerRegistrations = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
                .Where(x => x.Interface.IsGenericType &&
                            (x.Interface.GetGenericTypeDefinition() == commandHandlerType ||
                             x.Interface.GetGenericTypeDefinition() == queryHandlerType));

            foreach (var registration in handlerRegistrations)
                services.AddScoped(registration.Interface, registration.Type);
        }

        return services;
    }
}
