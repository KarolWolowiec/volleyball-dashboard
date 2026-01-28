using System.Text.Json.Serialization;

namespace VolleyballDashboard.Infrastructure.ApiClient.Dtos;

public record GroupStandingResponseDto
{
    [JsonPropertyName("roundType")]
    public string RoundType { get; init; } = string.Empty;
    
    [JsonPropertyName("teams")]
    public List<StandingDto> Teams { get; init; } = [];
}
