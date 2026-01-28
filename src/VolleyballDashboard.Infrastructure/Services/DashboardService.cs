using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VolleyballDashboard.Core.Interfaces;
using VolleyballDashboard.Core.Models;
using VolleyballDashboard.Infrastructure.Configuration;

namespace VolleyballDashboard.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IVolleyballApiClient _apiClient;
    private readonly ICacheService _cacheService;
    private readonly IMatchSchedulerService _matchScheduler;
    private readonly ITeamLogoCache _logoCache;
    private readonly ILogger<DashboardService> _logger;
    private readonly CacheSettings _cacheSettings;

    private const string StandingsCacheKey = "standings:{0}";
    private const string UpcomingMatchesCacheKey = "upcoming:{0}";
    private const string PreviousMatchesCacheKey = "previous:{0}";
    private const string ActiveMatchesCacheKey = "active:{0}";

    public DashboardService(
        IVolleyballApiClient apiClient,
        ICacheService cacheService,
        IMatchSchedulerService matchScheduler,
        ITeamLogoCache logoCache,
        IOptions<CacheSettings> cacheSettings,
        ILogger<DashboardService> logger)
    {
        _apiClient = apiClient;
        _cacheService = cacheService;
        _matchScheduler = matchScheduler;
        _logoCache = logoCache;
        _logger = logger;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<DashboardData> GetDashboardDataAsync(string leagueId, CancellationToken cancellationToken = default)
    {
        var league = GetLeague(leagueId);
        if (league is null)
            throw new ArgumentException($"League not found: {leagueId}", nameof(leagueId));

        // Fetch fixtures first to populate the logo cache (logos come from fixtures endpoint)
        var upcomingTask = GetUpcomingMatchesInternalAsync(leagueId, cancellationToken);
        var previousTask = GetPreviousMatchesInternalAsync(leagueId, cancellationToken);
        var activeTask = GetActiveMatchesInternalAsync(leagueId, cancellationToken);

        // Wait for fixtures to complete first so logo cache is populated
        await Task.WhenAll(upcomingTask, previousTask, activeTask);

        // Now fetch standings - they will use the populated logo cache
        var standings = await GetStandingsInternalAsync(leagueId, cancellationToken);

        return new DashboardData
        {
            League = league,
            Standings = standings,
            UpcomingMatches = await upcomingTask,
            PreviousMatches = await previousTask,
            ActiveMatches = await activeTask,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    public async Task RefreshStandingsAsync(string leagueId, CancellationToken cancellationToken = default)
    {
        var league = GetLeague(leagueId);
        if (league is null) return;

        var cacheKey = string.Format(StandingsCacheKey, leagueId);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        
        var standings = await _apiClient.GetStandingsAsync(league, cancellationToken);
        await _cacheService.SetAsync(cacheKey, standings, 
            TimeSpan.FromMinutes(_cacheSettings.StandingsCacheMinutes), cancellationToken);
        
        _logger.LogInformation("Refreshed standings for {LeagueId}", leagueId);
    }

    public async Task RefreshUpcomingMatchesAsync(string leagueId, CancellationToken cancellationToken = default)
    {
        var league = GetLeague(leagueId);
        if (league is null) return;

        var allMatches = await _apiClient.GetFixturesAsync(league, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var twoWeeksFuture = now.AddDays(14);

        var upcomingMatches = allMatches
            .Where(m => m.Status == MatchStatus.Upcoming && m.StartTime > now && m.StartTime <= twoWeeksFuture)
            .OrderBy(m => m.StartTime)
            .ToList();

        var upcomingCacheKey = string.Format(UpcomingMatchesCacheKey, leagueId);
        await _cacheService.SetAsync(upcomingCacheKey, upcomingMatches,
            TimeSpan.FromMinutes(_cacheSettings.UpcomingMatchesCacheMinutes), cancellationToken);

        // Schedule tracking for upcoming matches
        foreach (var match in upcomingMatches)
        {
            await _matchScheduler.ScheduleMatchTrackingAsync(match, leagueId, cancellationToken);
        }

        _logger.LogInformation("Refreshed {Count} upcoming matches for {LeagueId}", 
            upcomingMatches.Count, leagueId);
    }

    public async Task RefreshActiveMatchesAsync(string leagueId, CancellationToken cancellationToken = default)
    {
        var league = GetLeague(leagueId);
        if (league is null) return;

        var liveMatches = await _apiClient.GetLiveMatchesAsync(league, cancellationToken);
        
        var activeCacheKey = string.Format(ActiveMatchesCacheKey, leagueId);
        await _cacheService.SetAsync(activeCacheKey, liveMatches,
            TimeSpan.FromMinutes(_cacheSettings.ActiveMatchesCacheMinutes), cancellationToken);

        // Process finished matches
        var finishedMatches = liveMatches.Where(m => m.Status == MatchStatus.Finished).ToList();
        if (finishedMatches.Count != 0)
        {
            await ProcessFinishedMatchesAsync(leagueId, finishedMatches, cancellationToken);
        }

        _logger.LogInformation("Refreshed {Count} active matches for {LeagueId}", 
            liveMatches.Count(m => m.Status == MatchStatus.Live), leagueId);
    }

    public League? GetLeague(string leagueId) => LeagueConfiguration.GetLeague(leagueId);

    public IReadOnlyList<League> GetAvailableLeagues() => LeagueConfiguration.GetAllLeagues();

    private async Task<List<Standing>> GetStandingsInternalAsync(string leagueId, CancellationToken cancellationToken)
    {
        var league = GetLeague(leagueId)!;
        var cacheKey = string.Format(StandingsCacheKey, leagueId);
        
        var standings = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _apiClient.GetStandingsAsync(league, cancellationToken),
            TimeSpan.FromMinutes(_cacheSettings.StandingsCacheMinutes),
            cancellationToken);

        // Enrich standings with logos from cache (in case cached standings are missing logos)
        return standings.Select(s => EnrichStandingWithLogo(s)).ToList();
    }

    private Standing EnrichStandingWithLogo(Standing standing)
    {
        // If team already has a logo, return as-is
        if (!string.IsNullOrEmpty(standing.Team.LogoUrl))
            return standing;

        // Try to get logo from cache
        var logoUrl = _logoCache.GetLogo(standing.Team.Id);
        if (string.IsNullOrEmpty(logoUrl))
            return standing;

        // Create new standing with enriched team
        return standing with
        {
            Team = standing.Team with { LogoUrl = logoUrl }
        };
    }

    private async Task<List<Match>> GetUpcomingMatchesInternalAsync(string leagueId, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(UpcomingMatchesCacheKey, leagueId);
        var cached = await _cacheService.GetAsync<List<Match>>(cacheKey, cancellationToken);
        
        if (cached is not null)
        {
            // Filter out matches that have already started
            var now = DateTimeOffset.UtcNow;
            return cached.Where(m => m.StartTime > now).ToList();
        }

        // If no cache, fetch and populate
        await RefreshUpcomingMatchesAsync(leagueId, cancellationToken);
        cached = await _cacheService.GetAsync<List<Match>>(cacheKey, cancellationToken);
        return cached ?? [];
    }

    private async Task<List<Match>> GetPreviousMatchesInternalAsync(string leagueId, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(PreviousMatchesCacheKey, leagueId);
        var cached = await _cacheService.GetAsync<List<Match>>(cacheKey, cancellationToken);
        
        if (cached is not null)
            return cached;

        // Fetch from dedicated results endpoint
        var league = GetLeague(leagueId)!;
        var results = await _apiClient.GetResultsAsync(league, cancellationToken);

        await _cacheService.SetAsync(cacheKey, results,
            TimeSpan.FromMinutes(_cacheSettings.PreviousMatchesCacheMinutes), cancellationToken);

        _logger.LogInformation("Fetched {Count} previous results for {LeagueId}", results.Count, leagueId);

        return results;
    }

    private async Task<List<Match>> GetActiveMatchesInternalAsync(string leagueId, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(ActiveMatchesCacheKey, leagueId);
        var cached = await _cacheService.GetAsync<List<Match>>(cacheKey, cancellationToken);
        
        if (cached is not null)
            return cached.Where(m => m.Status == MatchStatus.Live).ToList();

        var league = GetLeague(leagueId)!;
        var liveMatches = await _apiClient.GetLiveMatchesAsync(league, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, liveMatches,
            TimeSpan.FromMinutes(_cacheSettings.ActiveMatchesCacheMinutes), cancellationToken);

        return liveMatches.Where(m => m.Status == MatchStatus.Live).ToList();
    }

    private async Task ProcessFinishedMatchesAsync(string leagueId, List<Match> finishedMatches, CancellationToken cancellationToken)
    {
        // Move finished matches to previous matches cache
        var previousCacheKey = string.Format(PreviousMatchesCacheKey, leagueId);
        var previousMatches = await _cacheService.GetAsync<List<Match>>(previousCacheKey, cancellationToken) ?? [];
        
        var now = DateTimeOffset.UtcNow;
        var twoWeeksPast = now.AddDays(-14);
        
        // Add new finished matches and filter old ones
        var updatedPrevious = previousMatches
            .Union(finishedMatches, new MatchIdComparer())
            .Where(m => m.StartTime >= twoWeeksPast)
            .OrderByDescending(m => m.StartTime)
            .ToList();

        await _cacheService.SetAsync(previousCacheKey, updatedPrevious,
            TimeSpan.FromMinutes(_cacheSettings.PreviousMatchesCacheMinutes), cancellationToken);

        // Remove from upcoming matches
        var upcomingCacheKey = string.Format(UpcomingMatchesCacheKey, leagueId);
        var upcomingMatches = await _cacheService.GetAsync<List<Match>>(upcomingCacheKey, cancellationToken);
        
        if (upcomingMatches is not null)
        {
            var finishedIds = finishedMatches.Select(m => m.Id).ToHashSet();
            var updatedUpcoming = upcomingMatches.Where(m => !finishedIds.Contains(m.Id)).ToList();
            await _cacheService.SetAsync(upcomingCacheKey, updatedUpcoming,
                TimeSpan.FromMinutes(_cacheSettings.UpcomingMatchesCacheMinutes), cancellationToken);
        }

        // Cancel tracking for finished matches
        foreach (var match in finishedMatches)
        {
            await _matchScheduler.CancelMatchTrackingAsync(match.Id, cancellationToken);
        }

        // Refresh standings after match completion
        await RefreshStandingsAsync(leagueId, cancellationToken);
        
        _logger.LogInformation("Processed {Count} finished matches for {LeagueId}", 
            finishedMatches.Count, leagueId);
    }

    private class MatchIdComparer : IEqualityComparer<Match>
    {
        public bool Equals(Match? x, Match? y) => x?.Id == y?.Id;
        public int GetHashCode(Match obj) => obj.Id.GetHashCode();
    }
}
