namespace VolleyballDashboard.Core.Interfaces;

public interface ITeamLogoCache
{
    void SetLogo(string teamId, string? logoUrl);
    void SetLogos(IEnumerable<(string TeamId, string? LogoUrl)> logos);
    string? GetLogo(string teamId);
    IReadOnlyDictionary<string, string> GetAllLogos();
}
