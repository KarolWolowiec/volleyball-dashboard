using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using VolleyballDashboard.Core.Interfaces;

namespace VolleyballDashboard.Infrastructure.Services;

public class TeamLogoCache : ITeamLogoCache
{
    private readonly ConcurrentDictionary<string, string> _logos = new();
    private readonly ILogger<TeamLogoCache> _logger;

    public TeamLogoCache(ILogger<TeamLogoCache> logger)
    {
        _logger = logger;
    }

    public void SetLogo(string teamId, string? logoUrl)
    {
        if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(logoUrl))
            return;

        if (_logos.TryAdd(teamId, logoUrl))
        {
            _logger.LogDebug("Cached logo for team {TeamId}", teamId);
        }
    }

    public void SetLogos(IEnumerable<(string TeamId, string? LogoUrl)> logos)
    {
        var count = 0;
        foreach (var (teamId, logoUrl) in logos)
        {
            if (!string.IsNullOrEmpty(teamId) && !string.IsNullOrEmpty(logoUrl))
            {
                if (_logos.TryAdd(teamId, logoUrl))
                {
                    count++;
                }
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Cached {Count} new team logos (total: {Total})", count, _logos.Count);
        }
    }

    public string? GetLogo(string teamId)
    {
        if (string.IsNullOrEmpty(teamId))
            return null;

        return _logos.TryGetValue(teamId, out var logoUrl) ? logoUrl : null;
    }

    public IReadOnlyDictionary<string, string> GetAllLogos() => _logos;
}
