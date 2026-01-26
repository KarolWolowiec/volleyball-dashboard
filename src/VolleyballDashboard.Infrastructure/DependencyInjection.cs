using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VolleyballDashboard.Core.Interfaces;
using VolleyballDashboard.Infrastructure.ApiClient;
using VolleyballDashboard.Infrastructure.BackgroundServices;
using VolleyballDashboard.Infrastructure.Configuration;
using VolleyballDashboard.Infrastructure.Services;

namespace VolleyballDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<ApiSettings>(configuration.GetSection(ApiSettings.SectionName));
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        // Memory cache
        services.AddMemoryCache();

        // HTTP client
        var apiSettings = configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>();
        services.AddHttpClient<IVolleyballApiClient, FlashscoreApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiSettings?.BaseUrl ?? "https://api.sportdb.dev");
            client.Timeout = TimeSpan.FromSeconds(apiSettings?.TimeoutSeconds ?? 30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "VolleyballDashboard/1.0");
            if (!string.IsNullOrEmpty(apiSettings?.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiSettings.ApiKey);
            }
        });

        // Services
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<ITeamLogoCache, TeamLogoCache>();
        services.AddSingleton<IMatchSchedulerService, MatchSchedulerService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Background services
        services.AddHostedService<WeeklyMatchRefreshService>();

        return services;
    }
}
