namespace VolleyballDashboard.Infrastructure.Configuration;

public class ApiSettings
{
    public const string SectionName = "ApiSettings";
    
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
