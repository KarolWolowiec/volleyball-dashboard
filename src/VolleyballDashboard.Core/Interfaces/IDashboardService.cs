using VolleyballDashboard.Core.Models;

namespace VolleyballDashboard.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync(string leagueId, CancellationToken cancellationToken = default);
    Task RefreshStandingsAsync(string leagueId, CancellationToken cancellationToken = default);
    Task RefreshUpcomingMatchesAsync(string leagueId, CancellationToken cancellationToken = default);
    Task RefreshActiveMatchesAsync(string leagueId, CancellationToken cancellationToken = default);
    League? GetLeague(string leagueId);
    IReadOnlyList<League> GetAvailableLeagues();
}
