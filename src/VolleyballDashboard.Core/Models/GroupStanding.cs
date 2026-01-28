namespace VolleyballDashboard.Core.Models;

public record GroupStanding
{
    public required string GroupName { get; init; }
    public required List<Standing> Standings { get; init; }
}
