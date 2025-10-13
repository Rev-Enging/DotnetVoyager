namespace DotnetVoyager.WebAPI.Settings;

public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
