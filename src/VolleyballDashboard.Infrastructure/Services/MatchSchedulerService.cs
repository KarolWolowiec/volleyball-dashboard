using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VolleyballDashboard.Core.Interfaces;
using VolleyballDashboard.Core.Models;

namespace VolleyballDashboard.Infrastructure.Services;

public class MatchSchedulerService : IMatchSchedulerService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchSchedulerService> _logger;
    private readonly ConcurrentDictionary<string, MatchTrackingJob> _activeJobs = new();
    private bool _disposed;

    public MatchSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<MatchSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task ScheduleMatchTrackingAsync(Match match, string leagueId, CancellationToken cancellationToken = default)
    {
        if (_activeJobs.ContainsKey(match.Id))
        {
            _logger.LogDebug("Match {MatchId} is already being tracked", match.Id);
            return Task.CompletedTask;
        }

        var now = DateTimeOffset.UtcNow;
        var delay = match.StartTime - now;

        if (delay <= TimeSpan.Zero)
        {
            // Match should already be starting, begin tracking immediately
            StartMatchTracking(match, leagueId);
        }
        else
        {
            // Schedule to start tracking when match begins
            var cts = new CancellationTokenSource();
            var job = new MatchTrackingJob
            {
                MatchId = match.Id,
                LeagueId = leagueId,
                CancellationTokenSource = cts,
                IsActive = false
            };

            if (_activeJobs.TryAdd(match.Id, job))
            {
                _ = ScheduleDelayedStart(match, leagueId, delay, cts.Token);
                _logger.LogInformation("Scheduled tracking for match {MatchId} in {Delay}", match.Id, delay);
            }
        }

        return Task.CompletedTask;
    }

    public Task CancelMatchTrackingAsync(string matchId, CancellationToken cancellationToken = default)
    {
        if (_activeJobs.TryRemove(matchId, out var job))
        {
            job.CancellationTokenSource.Cancel();
            job.CancellationTokenSource.Dispose();
            _logger.LogInformation("Cancelled tracking for match {MatchId}", matchId);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<string> GetTrackedMatchIds() => _activeJobs.Keys.ToList();

    private async Task ScheduleDelayedStart(Match match, string leagueId, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                StartMatchTracking(match, leagueId);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Delayed start cancelled for match {MatchId}", match.Id);
        }
    }

    private void StartMatchTracking(Match match, string leagueId)
    {
        if (_activeJobs.TryGetValue(match.Id, out var existingJob) && existingJob.IsActive)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        var job = new MatchTrackingJob
        {
            MatchId = match.Id,
            LeagueId = leagueId,
            CancellationTokenSource = cts,
            IsActive = true
        };

        _activeJobs.AddOrUpdate(match.Id, job, (_, oldJob) =>
        {
            oldJob.CancellationTokenSource.Cancel();
            oldJob.CancellationTokenSource.Dispose();
            return job;
        });

        _ = RunMatchTrackingLoop(match.Id, leagueId, cts.Token);
        _logger.LogInformation("Started live tracking for match {MatchId}", match.Id);
    }

    private async Task RunMatchTrackingLoop(string matchId, string leagueId, CancellationToken cancellationToken)
    {
        const int pollIntervalMinutes = 15;
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
                
                await dashboardService.RefreshActiveMatchesAsync(leagueId, cancellationToken);
                
                _logger.LogDebug("Polled live data for match {MatchId}", matchId);
                
                await Task.Delay(TimeSpan.FromMinutes(pollIntervalMinutes), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Tracking loop cancelled for match {MatchId}", matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tracking loop for match {MatchId}", matchId);
        }
        finally
        {
            _activeJobs.TryRemove(matchId, out _);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        foreach (var job in _activeJobs.Values)
        {
            job.CancellationTokenSource.Cancel();
            job.CancellationTokenSource.Dispose();
        }
        
        _activeJobs.Clear();
        _disposed = true;
    }

    private class MatchTrackingJob
    {
        public required string MatchId { get; init; }
        public required string LeagueId { get; init; }
        public required CancellationTokenSource CancellationTokenSource { get; init; }
        public required bool IsActive { get; set; }
    }
}
