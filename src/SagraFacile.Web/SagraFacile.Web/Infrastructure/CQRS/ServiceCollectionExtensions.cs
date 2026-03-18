using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SagraFacile.Web.Infrastructure.CQRS;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and all command/query handlers from the specified assembly
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        // Register the mediator
        services.AddScoped<IMediator, Mediator>();

        // Find and register all command handlers
        var commandHandlerType = typeof(ICommandHandler<,>);
        var queryHandlerType = typeof(IQueryHandler<,>);

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
            .Where(x => x.Interface.IsGenericType && 
                       (x.Interface.GetGenericTypeDefinition() == commandHandlerType ||
                        x.Interface.GetGenericTypeDefinition() == queryHandlerType))
            .ToList();

        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Type);
        }

        return services;
    }
}
