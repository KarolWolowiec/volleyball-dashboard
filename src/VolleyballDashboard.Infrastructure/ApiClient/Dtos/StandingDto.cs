using System.Text.Json.Serialization;

namespace VolleyballDashboard.Infrastructure.ApiClient.Dtos;

public record StandingDto
{
    [JsonPropertyName("rank")]
    public string Rank { get; init; } = string.Empty;
    
    [JsonPropertyName("teamId")]
    public string TeamId { get; init; } = string.Empty;
    
    [JsonPropertyName("teamName")]
    public string TeamName { get; init; } = string.Empty;
    
    [JsonPropertyName("teamSlug")]
    public string? TeamSlug { get; init; }
    
    [JsonPropertyName("matches")]
    public string Matches { get; init; } = "0";
    
    [JsonPropertyName("wins")]
    public string Wins { get; init; } = "0";
    
    [JsonPropertyName("winsRegular")]
    public string WinsRegular { get; init; } = "0";
    
    [JsonPropertyName("winsOvertime")]
    public string WinsOvertime { get; init; } = "0";
    
    [JsonPropertyName("lossesRegular")]
    public string LossesRegular { get; init; } = "0";
    
    [JsonPropertyName("lossesOvertime")]
    public string LossesOvertime { get; init; } = "0";
    
    [JsonPropertyName("points")]
    public string Points { get; init; } = "0";
    
    [JsonPropertyName("goals")]
    public string Goals { get; init; } = "0:0";
    
    [JsonPropertyName("goalDiff")]
    public string GoalDiff { get; init; } = "0";
    
    [JsonPropertyName("rankColor")]
    public string? RankColor { get; init; }
    
    [JsonPropertyName("rankClass")]
    public string? RankClass { get; init; }
}
