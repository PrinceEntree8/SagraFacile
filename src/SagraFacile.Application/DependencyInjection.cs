using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SagraFacile.Application.Infrastructure.CQRS;

namespace SagraFacile.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the custom mediator, all CQRS handlers, and FluentValidation validators
    /// from the Application assembly.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediator(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
