# 🏐 Volleyball Dashboard

A lightweight, modern volleyball dashboard built with .NET 8 Blazor and Tailwind CSS. Designed to run on Raspberry Pi and other ARM-based Linux systems.

![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![Docker](https://img.shields.io/badge/Docker-ARM64-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- 📊 **Live Standings** - Real-time league standings with automatic updates
- ⚡ **Live Matches** - Track ongoing matches with live score updates
- 📅 **Upcoming Matches** - View matches up to 2 weeks in advance
- 📜 **Recent Results** - Browse previous match results from the last 2 weeks
- 🌙 **Dark/Light Mode** - Toggle between themes for comfortable viewing
- 📱 **Mobile-First** - Responsive design that works on all devices
- 🚀 **Lightweight** - Optimized for Raspberry Pi with minimal resource usage

## Technical Stack

- **Backend**: .NET 8 with Blazor Server
- **Frontend**: Tailwind CSS (via CDN for lightweight deployment)
- **Caching**: In-memory cache (no database required)
- **Container**: Docker with ARM64 support
- **API**: sportdb.dev Flashscore API

## Quick Start

### 1. Set Up Your API Key

Before running the application, you need to configure your sportdb.dev API key:

```bash
# Copy the example environment file
cp env.example .env

# Edit .env and add your API key
# API_KEY=your_actual_api_key_here
```

> **Note**: The `.env` file is git-ignored and will not be committed to the repository.

### 2. Run with Docker (Recommended)

```bash
# Clone the repository
git clone <repository-url>
cd volleyball-dashboard

# Build and run with Docker Compose
docker compose up -d

# Access the dashboard
open http://localhost:8080
```

### 3. Manual Build (Alternative)

```bash
# Restore dependencies
dotnet restore

# Set up user secrets for local development (recommended)
dotnet user-secrets init --project src/VolleyballDashboard.Web
dotnet user-secrets set "ApiSettings:ApiKey" "your_api_key_here" --project src/VolleyballDashboard.Web

# Run in development mode
dotnet run --project src/VolleyballDashboard.Web

# Or build for production
dotnet publish src/VolleyballDashboard.Web -c Release -o ./publish
```

> **Tip**: User secrets are stored outside your project folder and are never committed to git.

## Building for Raspberry Pi

```bash
# Build multi-arch image
docker buildx build --platform linux/arm64 -t volleyball-dashboard:arm64 .

# Or build on the Pi directly
docker compose up -d --build
```

## Configuration

Configuration can be set via `appsettings.json` or environment variables:

| Setting | Environment Variable | Default | Description |
|---------|---------------------|---------|-------------|
| API Base URL | `ApiSettings__BaseUrl` | `https://api.sportdb.dev` | External API base URL |
| API Key | `ApiSettings__ApiKey` | - | Your API key |
| Standings Cache | `CacheSettings__StandingsCacheMinutes` | `60` | Cache duration for standings |
| Upcoming Cache | `CacheSettings__UpcomingMatchesCacheMinutes` | `10080` | Cache duration for upcoming matches |

### Environment Variables

Copy `env.example` to `.env` and configure your secrets:

```bash
cp env.example .env
```

```env
# Required: Your sportdb.dev API key
API_KEY=your_api_key_here
```

> **Security**: The `.env` file is listed in `.gitignore` and will not be committed to version control.

## Architecture

```
src/
├── VolleyballDashboard.Core/           # Domain models & interfaces
├── VolleyballDashboard.Infrastructure/ # API clients, caching, services
└── VolleyballDashboard.Web/           # Blazor frontend
```

### Key Design Decisions

1. **No Database**: Uses in-memory cache to minimize resource usage and complexity
2. **Smart Caching**: Standings update after matches, fixtures refresh weekly
3. **Background Jobs**: Automatic match tracking with 15-minute polling during live matches
4. **Rate Limiting Aware**: Minimizes API calls through intelligent caching

## Adding New Leagues

To add support for additional leagues, update `LeagueConfiguration.cs`:

```csharp
["newleague"] = new League
{
    Id = "newleague",
    Name = "New League Name",
    Country = "Country",
    CountryCode = "XXX",
    Season = "2025-2026",
    StandingsEndpoint = "/api/flashscore/volleyball/country:XXX/league:ID/season/standings",
    FixturesEndpoint = "/api/flashscore/volleyball/country:XXX/league:ID/season/fixtures",
    LiveEndpoint = "/api/flashscore/volleyball/country:XXX/league:ID/live"
}
```

## Resource Usage

Designed for Raspberry Pi 3B+ and newer:

- **Memory**: ~100-150MB RAM
- **CPU**: Minimal (< 5% during idle)
- **Storage**: ~50MB for Docker image

## API Rate Limiting

The dashboard is designed to minimize API calls:

- Standings: Cached for 60 minutes, refreshed after match completion
- Upcoming matches: Refreshed weekly (Monday 6 AM UTC)
- Live matches: Polled every 15 minutes during active matches
- Previous matches: Cached for 1 week

## Development

```bash
# Watch mode with hot reload
dotnet watch --project src/VolleyballDashboard.Web

# Run tests (if applicable)
dotnet test
```

## License

MIT License - feel free to use and modify for your own projects.

## Credits

- Data provided by [sportdb.dev](https://sportdb.dev)
- Icons by Heroicons
- Fonts: Outfit, JetBrains Mono
