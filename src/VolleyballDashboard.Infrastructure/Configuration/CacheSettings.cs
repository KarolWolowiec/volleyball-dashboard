namespace VolleyballDashboard.Infrastructure.Configuration;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";
    
    public int StandingsCacheMinutes { get; set; } = 60;
    public int UpcomingMatchesCacheMinutes { get; set; } = 10080; // 1 week
    public int PreviousMatchesCacheMinutes { get; set; } = 10080; // 1 week
    public int ActiveMatchesCacheMinutes { get; set; } = 5;
    public int DefaultCacheMinutes { get; set; } = 30;
}
