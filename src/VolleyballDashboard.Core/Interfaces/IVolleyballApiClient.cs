using VolleyballDashboard.Core.Models;

namespace VolleyballDashboard.Core.Interfaces;

public interface IVolleyballApiClient
{
    Task<List<Standing>> GetStandingsAsync(League league, CancellationToken cancellationToken = default);
    Task<List<GroupStanding>> GetGroupedStandingsAsync(League league, CancellationToken cancellationToken = default);
    Task<List<Match>> GetFixturesAsync(League league, CancellationToken cancellationToken = default);
    Task<List<Match>> GetResultsAsync(League league, CancellationToken cancellationToken = default);
    Task<List<Match>> GetLiveMatchesAsync(League league, CancellationToken cancellationToken = default);
}
