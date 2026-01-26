namespace VolleyballDashboard.Core.Models;

public record Team
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Slug { get; init; }
    public string? LogoUrl { get; init; }
    public string? ShortName { get; init; }
}
