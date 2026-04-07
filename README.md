# FastGuide

FastGuide is a production-oriented MVP for a FAST (Free Ad-Supported TV) guide built on modern .NET and following Microsoft best practices.

## What this repository contains

- **`FastGuide.Api`**: REST API with versioning (`/api/v1/channels`, `/api/v1/channels/{id}`, `/api/v1/now`, `/api/v1/search`), Swagger/OpenAPI docs, security headers, rate limiting, and backward-compatible legacy routes.
- **`FastGuide.Core`**: Domain models + normalization contracts/logic.
- **`FastGuide.Infrastructure`**: EF Core SQLite, provider clients, ingestion orchestration.
- **`FastGuide.Ingestion`**: Scheduled worker that runs ingestion every 15 minutes.
- **`FastGuide.ServiceDefaults`**: .NET Aspire service defaults (health checks, resilience, service discovery, OpenTelemetry hooks).
- **`FastGuide.AppHost`**: .NET Aspire app host used for local orchestration and service startup.
- **`FastGuide.Tests`**: xUnit tests for normalization, ingestion parsing resilience, and API integration smoke tests.

## Quick start

### Prerequisites

- .NET 10 SDK (latest stable).
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

The API will be available at `http://localhost:5000` with:
- **Swagger UI**: `http://localhost:5000/api-docs`
- **Health Check**: `http://localhost:5000/health`
- **Web Dashboard**: `http://localhost:5000/`

### Run ingestion worker only

```bash
dotnet run --project src/FastGuide.Ingestion/FastGuide.Ingestion.csproj
```

## Web UI

The API project now also serves a lightweight, dark-themed dashboard from `/` (`wwwroot`) inspired by cinematic FAST guide layouts. It includes:
- left navigation rail + top search
- live highlights (`/now`)
- channel directory (`/channels`)
- integrated search experience (`/search`)

## API endpoints

### Versioned Endpoints (Recommended)

- `GET /api/v1/channels` - List all channels
- `GET /api/v1/channels/{id}` - Get channel by ID
- `GET /api/v1/now` - Get currently playing programs
- `GET /api/v1/search?query={q}` - Search channels and programs

### Legacy Endpoints (Backward Compatibility)

- `GET /channels`
- `GET /channels/{id}`
- `GET /now`
- `GET /search?query={q}`

### API Documentation

Interactive API documentation available at `/api-docs` (Swagger UI) when running in Development mode.

See [docs/API.md](docs/API.md) for examples and detailed endpoint specifications.

## Architecture docs

- [Architecture overview](docs/ARCHITECTURE.md)
- [Ingestion pipeline details](docs/INGESTION.md)
- [Contributing guide](docs/CONTRIBUTING.md)
- [Security Policy](SECURITY.md)

## Configuration

- SQLite connection string: `ConnectionStrings:FastGuide`
- Startup ingestion toggle (API): `StartupIngestionEnabled` (default `true`)
- Standard OTLP exporter endpoint for Aspire/OpenTelemetry: `OTEL_EXPORTER_OTLP_ENDPOINT`
- Rate limiting: 100 requests per 60 seconds (configurable)
- CORS: Configured for localhost:3000, localhost:5173, and localhost (dev)

## Testing

```bash
dotnet test tests/FastGuide.Tests/FastGuide.Tests.csproj
```

## Docker

```bash
docker compose up --build
```

The Docker image includes:
- Multi-stage build for optimized size
- Non-root user execution (security)
- Health checks enabled
- Proper signal handling

## Security

FastGuide implements security best practices as documented in [SECURITY.md](SECURITY.md):

- ✅ Security headers (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, etc.)
- ✅ Rate limiting and DDoS protection
- ✅ Input validation on all DTOs
- ✅ Parameterized queries (SQL injection prevention)
- ✅ Global exception handling (information leakage prevention)
- ✅ Docker container runs as non-root user
- ✅ CORS policy enforcement
- ✅ OpenTelemetry observability for security monitoring

For security concerns, see [SECURITY.md](SECURITY.md).

## CI/CD

Automated build, test, and deployment pipelines via GitHub Actions:

- **Build & Test**: `.github/workflows/build-and-test.yml` - Runs on every push and PR
- **Docker Build**: `.github/workflows/docker-build.yml` - Builds and scans Docker images
- **Security Scanning**: Trivy scans for vulnerabilities in code and containers

## Dependency management

- Solution targets `net10.0` globally (`Directory.Build.props`)
- NuGet packages use specific version ranges for stability
- Package lock files (`packages.lock.json`) enforce reproducible builds
- Dependabot scans for known vulnerabilities
- All external dependencies are monitored for CVEs

## Code quality

- EditorConfig enforces consistent code style
- Nullable reference types enabled for null-safety
- Implicit usings reduce boilerplate
- Preview C# language features for modern code

## Best practices

This project follows [Microsoft .NET Best Practices](https://learn.microsoft.com/en-us/dotnet/core/) including:

- ✅ Dependency injection patterns
- ✅ Configuration management
- ✅ Structured logging with Serilog
- ✅ Entity Framework Core best practices
- ✅ OpenTelemetry instrumentation
- ✅ Health checks
- ✅ Resilience patterns
- ✅ Containerization guidelines
