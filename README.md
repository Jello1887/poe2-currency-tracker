# POE2 Market Watcher

A local .NET market watcher for Path of Exile 2 currency exchange data.

Designed to fetch currency exchange snapshots from the
official Grinding Gear Games API, store market history in SQLite, and publish
Discord digests and anomaly alerts for notable price and volume movement.

> Status: early development. The repository currently contains the solution
> structure and test projects; the market watcher workflow is still being built.

## Features

Planned MVP features:

- Official Path of Exile 2 Currency Exchange API integration
- OAuth using the `service:cxapi` scope
- SQLite storage for raw snapshots and derived pair metrics
- Discord webhook digests for each market run
- Top-volume pair selection for digest summaries
- Alert scanning across all available currency pairs
- Configurable sensitivity thresholds, cooldowns, and alert caps

## Project Structure

```text
PoeMarketWatcher.sln
src/
  PoeMarketWatcher.App/
  PoeMarketWatcher.Core/
  PoeMarketWatcher.Infrastructure/
tests/
  PoeMarketWatcher.Core.Tests/
  PoeMarketWatcher.Infrastructure.Tests/
```

### Projects

- `PoeMarketWatcher.App` - console entry point, configuration, and command flow
- `PoeMarketWatcher.Core` - market models, digest selection, anomaly scoring,
  and alert dedupe logic
- `PoeMarketWatcher.Infrastructure` - GGG API, OAuth, SQLite, and Discord
  integrations
- `PoeMarketWatcher.Core.Tests` - unit tests for market logic
- `PoeMarketWatcher.Infrastructure.Tests` - tests for API parsing, persistence,
  and webhook payloads

## Requirements

- .NET SDK 10.0.301 or compatible
- Grinding Gear Games OAuth client with `service:cxapi` access
- Discord webhook URL
- SQLite-compatible local environment

## Quick Start

Clone the repository and restore packages:

```powershell
dotnet restore
```

Build and test:

```powershell
dotnet build
dotnet test
```

Run the app:

```powershell
dotnet run --project src/PoeMarketWatcher.App -- once
```

The `once` mode is intended to perform one complete market watcher cycle and
then exit.

## Configuration

Configuration will be loaded from standard .NET configuration sources such as
`appsettings.json`, user secrets, environment variables, and ignored local
settings files.

Expected settings:

- GGG client ID
- GGG client secret
- GGG contact or user-agent details
- Discord webhook URL
- SQLite database path
- digest pair count
- alert thresholds
- alert cooldown window
- per-run alert cap

Secrets, webhook URLs, local databases, and machine-specific settings should not
be committed.

## Data Sources

This project is intended to use only official Grinding Gear Games APIs. The MVP
does not scrape trade-site pages or call undocumented browser endpoints.

## Roadmap

- Wire the `once` command
- Add configuration binding and validation
- Implement GGG OAuth token handling
- Fetch and parse currency exchange snapshots
- Persist snapshots and pair metrics in SQLite
- Generate market digests
- Publish Discord webhook messages
- Add anomaly alerts and dedupe rules

## License

No license has been selected.
