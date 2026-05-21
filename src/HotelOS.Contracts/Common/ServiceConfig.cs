namespace HotelOS.Contracts.Common;

/// <summary>
/// Single place that resolves the broker URL. Defaults to localhost but can be
/// overridden with the BROKER_URL environment variable (12-factor config).
/// </summary>
public static class ServiceConfig
{
    public static string BrokerUrl =>
        Environment.GetEnvironmentVariable("BROKER_URL") ?? "ws://localhost:5000/broker";

    /// <summary>Shared dashboard access token (see Task 3 — Authentication).</summary>
    public static string DashboardToken =>
        Environment.GetEnvironmentVariable("DASHBOARD_TOKEN") ?? "grandstay2026";
}
