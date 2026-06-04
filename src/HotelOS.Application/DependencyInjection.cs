using System.Reflection;
using FluentValidation;
using HotelOS.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.Application;

/// <summary>Registers MediatR handlers, FluentValidation validators and pipeline behaviors.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
