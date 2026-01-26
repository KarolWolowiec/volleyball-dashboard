namespace VolleyballDashboard.Core.Models;

public record Standing
{
    public required int Rank { get; init; }
    public required Team Team { get; init; }
    public required int Matches { get; init; }
    public required int Wins { get; init; }
    public required int Losses { get; init; }
    public required int Points { get; init; }
    public required string SetsRatio { get; init; }
    public required int SetsDiff { get; init; }
    public string? RankColor { get; init; }
    public string? RankClass { get; init; }
}
