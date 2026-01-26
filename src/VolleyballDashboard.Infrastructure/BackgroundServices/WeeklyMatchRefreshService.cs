using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VolleyballDashboard.Core.Interfaces;
using VolleyballDashboard.Infrastructure.Configuration;

namespace VolleyballDashboard.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that refreshes upcoming matches at the beginning of each week
/// </summary>
public class WeeklyMatchRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyMatchRefreshService> _logger;

    public WeeklyMatchRefreshService(
        IServiceProvider serviceProvider,
        ILogger<WeeklyMatchRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Weekly match refresh service started");

        // Initial load on startup
        await RefreshAllLeaguesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextMonday = GetNextWeekday(now, DayOfWeek.Monday);
                var delay = nextMonday - now;

                _logger.LogInformation("Next weekly refresh scheduled for {NextRefresh} (in {Delay})", 
                    nextMonday, delay);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RefreshAllLeaguesAsync(stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in weekly match refresh service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Weekly match refresh service stopped");
    }

    private async Task RefreshAllLeaguesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();

        foreach (var league in LeagueConfiguration.GetAllLeagues())
        {
            try
            {
                await dashboardService.RefreshUpcomingMatchesAsync(league.Id, cancellationToken);
                _logger.LogInformation("Refreshed upcoming matches for {LeagueId}", league.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh matches for {LeagueId}", league.Id);
            }
        }
    }

    private static DateTime GetNextWeekday(DateTime current, DayOfWeek targetDay)
    {
        var daysUntilTarget = ((int)targetDay - (int)current.DayOfWeek + 7) % 7;
        
        // If today is the target day and it's early morning, still use today
        if (daysUntilTarget == 0 && current.Hour >= 6)
        {
            daysUntilTarget = 7;
        }
        else if (daysUntilTarget == 0)
        {
            // It's early morning on target day, use today at 6 AM
            return current.Date.AddHours(6);
        }

        return current.Date.AddDays(daysUntilTarget).AddHours(6); // 6 AM UTC
    }
}
