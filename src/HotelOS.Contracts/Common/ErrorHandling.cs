using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HotelOS.Contracts.Common;

/// <summary>
/// Catches every unhandled exception so the system NEVER returns a raw stack
/// trace to a caller (Task 3 — Error handling). Validation failures become a
/// safe 400; anything else is logged internally and returned as a generic 500.
/// </summary>
public static class ErrorHandling
{
    public static IApplicationBuilder UseSafeErrors(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (ValidationException vex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = vex.Message });
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                logger?.CreateLogger("SafeErrors")
                       .LogError(ex, "Unhandled error on {Path}", context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "An internal error occurred. Please try again." });
            }
        });
    }
}
