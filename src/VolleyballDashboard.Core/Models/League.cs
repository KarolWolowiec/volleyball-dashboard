namespace VolleyballDashboard.Core.Models;

public record League
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Country { get; init; }
    public required string CountryCode { get; init; }
    public required string Season { get; init; }
    public required string StandingsEndpoint { get; init; }
    public required string FixturesEndpoint { get; init; }
    public required string ResultsEndpoint { get; init; }
    public required string LiveEndpoint { get; init; }
}
