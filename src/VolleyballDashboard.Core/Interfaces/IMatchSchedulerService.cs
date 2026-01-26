using VolleyballDashboard.Core.Models;

namespace VolleyballDashboard.Core.Interfaces;

public interface IMatchSchedulerService
{
    Task ScheduleMatchTrackingAsync(Match match, string leagueId, CancellationToken cancellationToken = default);
    Task CancelMatchTrackingAsync(string matchId, CancellationToken cancellationToken = default);
    IReadOnlyCollection<string> GetTrackedMatchIds();
}
