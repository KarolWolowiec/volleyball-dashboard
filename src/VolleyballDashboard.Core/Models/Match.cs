namespace VolleyballDashboard.Core.Models;

public record Match
{
    public required string Id { get; init; }
    public required Team HomeTeam { get; init; }
    public required Team AwayTeam { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required MatchStatus Status { get; init; }
    public string? Round { get; init; }
    public MatchScore? Score { get; init; }
    public List<SetScore>? SetScores { get; init; }
}

public record MatchScore
{
    public required int HomeScore { get; init; }
    public required int AwayScore { get; init; }
}

public record SetScore
{
    public required int SetNumber { get; init; }
    public required int HomeScore { get; init; }
    public required int AwayScore { get; init; }
}

public enum MatchStatus
{
    Upcoming,
    Live,
    Finished
}
