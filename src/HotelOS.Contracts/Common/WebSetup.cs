using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.Contracts.Common;

/// <summary>
/// Shared web wiring so every service serves its own browser UI the same way:
/// permissive CORS (so the guest portal can call services cross-origin in dev)
/// plus static-file hosting of the service's wwwroot/index.html.
/// </summary>
public static class WebSetup
{
    public static IServiceCollection AddHotelUi(this IServiceCollection services)
    {
        services.AddCors();
        // Enums as strings in REST responses too (matches the broker payloads).
        services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return services;
    }

    public static WebApplication UseHotelUi(this WebApplication app)
    {
        app.UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        app.UseDefaultFiles();
        app.UseStaticFiles();
        return app;
    }
}
