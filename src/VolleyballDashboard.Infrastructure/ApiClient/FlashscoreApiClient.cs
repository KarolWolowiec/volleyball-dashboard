using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VolleyballDashboard.Core.Interfaces;
using VolleyballDashboard.Core.Models;
using VolleyballDashboard.Infrastructure.ApiClient.Dtos;
using VolleyballDashboard.Infrastructure.Configuration;

namespace VolleyballDashboard.Infrastructure.ApiClient;

public class FlashscoreApiClient : IVolleyballApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITeamLogoCache _logoCache;
    private readonly ILogger<FlashscoreApiClient> _logger;
    private readonly ApiSettings _settings;

    public FlashscoreApiClient(
        HttpClient httpClient,
        ITeamLogoCache logoCache,
        IOptions<ApiSettings> settings,
        ILogger<FlashscoreApiClient> logger)
    {
        _httpClient = httpClient;
        _logoCache = logoCache;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<List<Standing>> GetStandingsAsync(League league, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching standings from {Url}", league.StandingsEndpoint);
            
            var response = await _httpClient.GetFromJsonAsync<List<StandingDto>>(league.StandingsEndpoint, cancellationToken);
            
            if (response is null)
            {
                _logger.LogWarning("No standings data received");
                return [];
            }

            return response.Select(dto => MapToStandingWithLogo(dto, _logoCache)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching standings for {LeagueId}", league.Id);
            throw;
        }
    }

    public async Task<List<GroupStanding>> GetGroupedStandingsAsync(League league, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching grouped standings from {Url}", league.StandingsEndpoint);
            
            var response = await _httpClient.GetFromJsonAsync<List<GroupStandingResponseDto>>(league.StandingsEndpoint, cancellationToken);
            
            if (response is null)
            {
                _logger.LogWarning("No grouped standings data received");
                return [];
            }

            return response.Select(group => new GroupStanding
            {
                GroupName = group.RoundType,
                Standings = group.Teams.Select(dto => MapToStandingWithLogo(dto, _logoCache)).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching grouped standings for {LeagueId}", league.Id);
            throw;
        }
    }

    public async Task<List<Match>> GetFixturesAsync(League league, CancellationToken cancellationToken = default)
    {
        try
        {
            var allFixtures = new List<FixtureDto>();
            var page = 1;
            const int maxPages = 5;

            while (page <= maxPages)
            {
                var url = $"{league.FixturesEndpoint}?page={page}";
                _logger.LogInformation("Fetching fixtures page {Page} from {Endpoint}", page, league.FixturesEndpoint);
                
                var response = await _httpClient.GetFromJsonAsync<List<FixtureDto>>(url, cancellationToken);
                
                if (response is null || response.Count == 0)
                    break;

                allFixtures.AddRange(response);
                
                // Check if we have enough fixtures for 2 weeks
                var twoWeeksFromNow = DateTimeOffset.UtcNow.AddDays(14);
                var latestFixture = allFixtures
                    .Where(f => TryParseUnixTimestamp(f.StartTime, out var dt))
                    .Select(f => { TryParseUnixTimestamp(f.StartTime, out var dt); return dt; })
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                if (latestFixture > twoWeeksFromNow)
                    break;

                page++;
            }

            // Cache team logos from fixtures (which include logo URLs)
            CacheLogosFromFixtures(allFixtures);

            var now = DateTimeOffset.UtcNow;
            var twoWeeksFuture = now.AddDays(14);
            var twoWeeksPast = now.AddDays(-14);

            return allFixtures
                .Select(MapToMatch)
                .Where(m => m.StartTime >= twoWeeksPast && m.StartTime <= twoWeeksFuture)
                .OrderBy(m => m.StartTime)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fixtures for {LeagueId}", league.Id);
            throw;
        }
    }

    public async Task<List<Match>> GetResultsAsync(League league, CancellationToken cancellationToken = default)
    {
        try
        {
            var allResults = new List<FixtureDto>();
            var page = 1;
            const int maxPages = 3; // Results are sorted by most recent, so we don't need many pages

            while (page <= maxPages)
            {
                var url = $"{league.ResultsEndpoint}?page={page}";
                _logger.LogInformation("Fetching results page {Page} from {Endpoint}", page, league.ResultsEndpoint);
                
                var response = await _httpClient.GetFromJsonAsync<List<FixtureDto>>(url, cancellationToken);
                
                if (response is null || response.Count == 0)
                    break;

                allResults.AddRange(response);
                
                // Check if we have enough results from last 2 weeks
                var twoWeeksAgo = DateTimeOffset.UtcNow.AddDays(-14);
                var oldestResult = allResults
                    .Where(f => TryParseUnixTimestamp(f.StartTime, out _))
                    .Select(f => { TryParseUnixTimestamp(f.StartTime, out var dt); return dt; })
                    .OrderBy(d => d)
                    .FirstOrDefault();

                if (oldestResult < twoWeeksAgo)
                    break;

                page++;
            }

            // Results endpoint may not return logos, so apply cached logos
            var now = DateTimeOffset.UtcNow;
            var twoWeeksPast = now.AddDays(-14);

            return allResults
                .Select(dto => MapToMatchWithCachedLogos(dto, _logoCache))
                .Where(m => m.StartTime >= twoWeeksPast && m.StartTime <= now)
                .OrderByDescending(m => m.StartTime)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching results for {LeagueId}", league.Id);
            throw;
        }
    }

    public async Task<List<Match>> GetLiveMatchesAsync(League league, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching live matches from {Url}", league.LiveEndpoint);
            
            var response = await _httpClient.GetFromJsonAsync<List<FixtureDto>>(league.LiveEndpoint, cancellationToken);
            
            if (response is null)
            {
                _logger.LogInformation("No live matches currently");
                return [];
            }

            // Live endpoint may have logos, but use cache as fallback
            return response.Select(dto => MapToMatchWithCachedLogos(dto, _logoCache)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching live matches for {LeagueId}", league.Id);
            throw;
        }
    }

    private void CacheLogosFromFixtures(List<FixtureDto> fixtures)
    {
        var logos = fixtures
            .SelectMany(f => new[]
            {
                (TeamId: f.HomeParticipantIds, LogoUrl: f.HomeLogo),
                (TeamId: f.AwayParticipantIds, LogoUrl: f.AwayLogo)
            })
            .Where(x => !string.IsNullOrEmpty(x.TeamId) && !string.IsNullOrEmpty(x.LogoUrl))
            .DistinctBy(x => x.TeamId);

        _logoCache.SetLogos(logos);
    }

    private static Standing MapToStandingWithLogo(StandingDto dto, ITeamLogoCache logoCache)
    {
        var totalLosses = int.TryParse(dto.LossesRegular, out var lr) ? lr : 0;
        totalLosses += int.TryParse(dto.LossesOvertime, out var lo) ? lo : 0;

        return new Standing
        {
            Rank = int.TryParse(dto.Rank, out var rank) ? rank : 0,
            Team = new Team
            {
                Id = dto.TeamId,
                Name = dto.TeamName,
                Slug = dto.TeamSlug,
                LogoUrl = logoCache.GetLogo(dto.TeamId) // Look up from cache
            },
            Matches = int.TryParse(dto.Matches, out var matches) ? matches : 0,
            Wins = int.TryParse(dto.Wins, out var wins) ? wins : 0,
            Losses = totalLosses,
            Points = int.TryParse(dto.Points, out var points) ? points : 0,
            SetsRatio = dto.Goals,
            SetsDiff = int.TryParse(dto.GoalDiff, out var diff) ? diff : 0,
            RankColor = dto.RankColor,
            RankClass = dto.RankClass
        };
    }

    private static Match MapToMatch(FixtureDto dto)
    {
        TryParseUnixTimestamp(dto.StartTime, out var startTime);
        
        var status = DetermineMatchStatus(dto);
        var score = ParseScore(dto);
        var setScores = ParseSetScores(dto);

        return new Match
        {
            Id = dto.EventId,
            HomeTeam = new Team
            {
                Id = dto.HomeParticipantIds,
                Name = dto.HomeName,
                LogoUrl = dto.HomeLogo,
                ShortName = dto.Home3CharName
            },
            AwayTeam = new Team
            {
                Id = dto.AwayParticipantIds,
                Name = dto.AwayName,
                LogoUrl = dto.AwayLogo,
                ShortName = dto.Away3CharName
            },
            StartTime = startTime,
            Status = status,
            Round = dto.Round,
            Score = score,
            SetScores = setScores
        };
    }

    private static Match MapToMatchWithCachedLogos(FixtureDto dto, ITeamLogoCache logoCache)
    {
        TryParseUnixTimestamp(dto.StartTime, out var startTime);
        
        var status = DetermineMatchStatus(dto);
        var score = ParseScore(dto);
        var setScores = ParseSetScores(dto);

        // Always prefer cached logos (from fixtures endpoint) as they are known to work
        var homeLogo = logoCache.GetLogo(dto.HomeParticipantIds) ?? dto.HomeLogo;
        var awayLogo = logoCache.GetLogo(dto.AwayParticipantIds) ?? dto.AwayLogo;

        return new Match
        {
            Id = dto.EventId,
            HomeTeam = new Team
            {
                Id = dto.HomeParticipantIds,
                Name = dto.HomeName,
                LogoUrl = homeLogo,
                ShortName = dto.Home3CharName
            },
            AwayTeam = new Team
            {
                Id = dto.AwayParticipantIds,
                Name = dto.AwayName,
                LogoUrl = awayLogo,
                ShortName = dto.Away3CharName
            },
            StartTime = startTime,
            Status = status,
            Round = dto.Round,
            Score = score,
            SetScores = setScores
        };
    }

    private static MatchStatus DetermineMatchStatus(FixtureDto dto)
    {
        var stageId = dto.EventStageId;
        var stage = dto.EventStage?.ToUpperInvariant() ?? "";

        if (stage == "FINISHED" || stageId == "3")
            return MatchStatus.Finished;
        
        if (stage == "SCHEDULED" || stageId == "1")
            return MatchStatus.Upcoming;

        // If it's not finished and not scheduled, assume it's live
        return MatchStatus.Live;
    }

    private static MatchScore? ParseScore(FixtureDto dto)
    {
        var homeScoreStr = dto.HomeFullTimeScore ?? dto.HomeScore;
        var awayScoreStr = dto.AwayFullTimeScore ?? dto.AwayScore;

        if (string.IsNullOrEmpty(homeScoreStr) || string.IsNullOrEmpty(awayScoreStr))
            return null;

        if (!int.TryParse(homeScoreStr, out var homeScore) || 
            !int.TryParse(awayScoreStr, out var awayScore))
            return null;

        return new MatchScore
        {
            HomeScore = homeScore,
            AwayScore = awayScore
        };
    }

    private static List<SetScore>? ParseSetScores(FixtureDto dto)
    {
        var setScores = new List<SetScore>();

        var periods = new[]
        {
            (dto.HomeResultPeriod1, dto.AwayResultPeriod1),
            (dto.HomeResultPeriod2, dto.AwayResultPeriod2),
            (dto.HomeResultPeriod3, dto.AwayResultPeriod3),
            (dto.HomeResultPeriod4, dto.AwayResultPeriod4),
            (dto.HomeResultPeriod5, dto.AwayResultPeriod5)
        };

        for (var i = 0; i < periods.Length; i++)
        {
            var (home, away) = periods[i];
            if (!string.IsNullOrEmpty(home) && !string.IsNullOrEmpty(away) &&
                int.TryParse(home, out var homeScore) && int.TryParse(away, out var awayScore))
            {
                setScores.Add(new SetScore
                {
                    SetNumber = i + 1,
                    HomeScore = homeScore,
                    AwayScore = awayScore
                });
            }
        }

        return setScores.Count > 0 ? setScores : null;
    }

    private static bool TryParseUnixTimestamp(string? timestamp, out DateTimeOffset result)
    {
        result = DateTimeOffset.MinValue;
        
        if (string.IsNullOrEmpty(timestamp))
            return false;

        if (long.TryParse(timestamp, out var unixSeconds))
        {
            result = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }

        return false;
    }
}
