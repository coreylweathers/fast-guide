# FastGuide

FastGuide is a production-oriented MVP for a FAST (Free Ad-Supported TV) guide built on modern .NET.

## What this repository contains

- **`FastGuide.Api`**: Minimal REST API (`/channels`, `/channels/{id}`, `/now`, `/search`).
- **`FastGuide.Core`**: Domain models + normalization contracts/logic.
- **`FastGuide.Infrastructure`**: EF Core SQLite, provider clients, ingestion orchestration.
- **`FastGuide.Ingestion`**: Scheduled worker that runs ingestion every 15 minutes.
- **`FastGuide.ServiceDefaults`**: .NET Aspire service defaults (health checks, resilience, service discovery, OpenTelemetry hooks).
- **`FastGuide.AppHost`**: .NET Aspire app host used for local orchestration and service startup.
- **`FastGuide.Tests`**: xUnit tests for normalization, ingestion parsing resilience, and API integration smoke tests.

## Quick start

### Prerequisites

- .NET SDK (latest stable).
- Docker (optional, for containerized execution).

### Run with .NET Aspire (recommended)

```bash
dotnet run --project src/FastGuide.AppHost/FastGuide.AppHost.csproj
```

This launches the API and ingestion worker together in an Aspire-managed local environment.

### Run API only

```bash
dotnet run --project src/FastGuide.Api/FastGuide.Api.csproj
```

### Run ingestion worker only

```bash
dotnet run --project src/FastGuide.Ingestion/FastGuide.Ingestion.csproj
```

## API endpoints

- `GET /channels`
- `GET /channels/{id}`
- `GET /now`
- `GET /search?query={q}`

See [docs/API.md](docs/API.md) for examples.

## Architecture docs

- [Architecture overview](docs/ARCHITECTURE.md)
- [Ingestion pipeline details](docs/INGESTION.md)
- [Contributing guide](docs/CONTRIBUTING.md)

## Configuration

- SQLite connection string: `ConnectionStrings:FastGuide`
- Startup ingestion toggle (API): `StartupIngestionEnabled` (default `true`)
- Standard OTLP exporter endpoint for Aspire/OpenTelemetry: `OTEL_EXPORTER_OTLP_ENDPOINT`

## Testing

```bash
dotnet test tests/FastGuide.Tests/FastGuide.Tests.csproj
```

## Docker

```bash
docker compose up --build
```
