namespace VolleyballDashboard.Core.Models;

public record DashboardData
{
    public required League League { get; init; }
    public required List<Standing> Standings { get; init; }
    public required List<Match> UpcomingMatches { get; init; }
    public required List<Match> ActiveMatches { get; init; }
    public required List<Match> PreviousMatches { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
}
