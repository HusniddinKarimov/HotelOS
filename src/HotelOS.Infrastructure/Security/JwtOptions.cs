namespace HotelOS.Infrastructure.Security;

/// <summary>JWT configuration bound from the "Jwt" section of appsettings.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "HotelOS";
    public string Audience { get; set; } = "HotelOS";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
