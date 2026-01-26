using System.Text.Json.Serialization;

namespace VolleyballDashboard.Infrastructure.ApiClient.Dtos;

public record FixtureDto
{
    [JsonPropertyName("eventId")]
    public string EventId { get; init; } = string.Empty;
    
    [JsonPropertyName("homeParticipantIds")]
    public string HomeParticipantIds { get; init; } = string.Empty;
    
    [JsonPropertyName("homeName")]
    public string HomeName { get; init; } = string.Empty;
    
    [JsonPropertyName("homeFirstName")]
    public string? HomeFirstName { get; init; }
    
    [JsonPropertyName("home3CharName")]
    public string? Home3CharName { get; init; }
    
    [JsonPropertyName("homeLogo")]
    public string? HomeLogo { get; init; }
    
    [JsonPropertyName("awayParticipantIds")]
    public string AwayParticipantIds { get; init; } = string.Empty;
    
    [JsonPropertyName("awayName")]
    public string AwayName { get; init; } = string.Empty;
    
    [JsonPropertyName("awayFirstName")]
    public string? AwayFirstName { get; init; }
    
    [JsonPropertyName("away3CharName")]
    public string? Away3CharName { get; init; }
    
    [JsonPropertyName("awayLogo")]
    public string? AwayLogo { get; init; }
    
    [JsonPropertyName("startTime")]
    public string StartTime { get; init; } = string.Empty;
    
    [JsonPropertyName("startDateTimeUtc")]
    public string? StartDateTimeUtc { get; init; }
    
    [JsonPropertyName("eventStage")]
    public string EventStage { get; init; } = string.Empty;
    
    [JsonPropertyName("eventStageId")]
    public string EventStageId { get; init; } = string.Empty;
    
    [JsonPropertyName("round")]
    public string? Round { get; init; }
    
    [JsonPropertyName("homeScore")]
    public string? HomeScore { get; init; }
    
    [JsonPropertyName("awayScore")]
    public string? AwayScore { get; init; }
    
    [JsonPropertyName("homeFullTimeScore")]
    public string? HomeFullTimeScore { get; init; }
    
    [JsonPropertyName("awayFullTimeScore")]
    public string? AwayFullTimeScore { get; init; }
    
    [JsonPropertyName("homeResultPeriod1")]
    public string? HomeResultPeriod1 { get; init; }
    
    [JsonPropertyName("awayResultPeriod1")]
    public string? AwayResultPeriod1 { get; init; }
    
    [JsonPropertyName("homeResultPeriod2")]
    public string? HomeResultPeriod2 { get; init; }
    
    [JsonPropertyName("awayResultPeriod2")]
    public string? AwayResultPeriod2 { get; init; }
    
    [JsonPropertyName("homeResultPeriod3")]
    public string? HomeResultPeriod3 { get; init; }
    
    [JsonPropertyName("awayResultPeriod3")]
    public string? AwayResultPeriod3 { get; init; }
    
    [JsonPropertyName("homeResultPeriod4")]
    public string? HomeResultPeriod4 { get; init; }
    
    [JsonPropertyName("awayResultPeriod4")]
    public string? AwayResultPeriod4 { get; init; }
    
    [JsonPropertyName("homeResultPeriod5")]
    public string? HomeResultPeriod5 { get; init; }
    
    [JsonPropertyName("awayResultPeriod5")]
    public string? AwayResultPeriod5 { get; init; }
}
