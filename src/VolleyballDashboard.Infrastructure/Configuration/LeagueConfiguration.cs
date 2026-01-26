using VolleyballDashboard.Core.Models;

namespace VolleyballDashboard.Infrastructure.Configuration;

public static class LeagueConfiguration
{
    private static readonly Dictionary<string, League> _leagues = new()
    {
        ["plusliga"] = new League
        {
            Id = "plusliga",
            Name = "PlusLiga",
            Country = "Poland",
            CountryCode = "154",
            Season = "2025-2026",
            StandingsEndpoint = "/api/flashscore/volleyball/poland:154/plusliga:jNqF318i/2025-2026/standings",
            FixturesEndpoint = "/api/flashscore/volleyball/poland:154/plusliga:jNqF318i/2025-2026/fixtures",
            ResultsEndpoint = "/api/flashscore/volleyball/poland:154/plusliga:jNqF318i/2025-2026/results",
            LiveEndpoint = "/api/flashscore/volleyball/poland:154/plusliga:jNqF318i/live"
        }
        // Add more leagues here in the future
    };

    public static League? GetLeague(string leagueId) => 
        _leagues.TryGetValue(leagueId.ToLowerInvariant(), out var league) ? league : null;

    public static IReadOnlyList<League> GetAllLeagues() => _leagues.Values.ToList();

    public static string DefaultLeagueId => "plusliga";
}
