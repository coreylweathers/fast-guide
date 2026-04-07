# Architecture

## High-level design

```text
                       +-------------------------+
                       |  FastGuide.AppHost      |
                       |  (.NET Aspire)          |
                       +-----------+-------------+
                                   |
            +----------------------+----------------------+
            |                                             |
+-----------v-------------+                   +-----------v-------------+
| FastGuide.Api           |                   | FastGuide.Ingestion     |
| Minimal APIs            |                   | BackgroundService        |
| DTOs + Query Endpoints  |                   | Scheduled every 15 min   |
+-----------+-------------+                   +-----------+-------------+
            |                                             |
            +----------------------+----------------------+
                                   |
                     +-------------v-------------+
                     | FastGuide.Infrastructure  |
                     | EF Core + Provider Clients|
                     | IngestionOrchestrator     |
                     +-------------+-------------+
                                   |
                     +-------------v-------------+
                     | FastGuide.Core            |
                     | Domain + Normalization    |
                     +---------------------------+
```

## Layer responsibilities

### Core

- Defines the shared domain entities and interfaces.
- Contains normalization logic (canonicalization + fuzzy dedupe + provider priority).
- No external I/O or persistence concerns.

### Infrastructure

- Implements EF Core persistence and provider ingestion clients.
- Encapsulates provider endpoint parsing and retry behavior.
- Runs orchestration that maps provider payloads to normalized entities.

### API

- Query-only boundary.
- Exposes typed DTOs and avoids leaking provider internals.
- Startup normalization refresh can be toggled for test environments.

### Ingestion worker

- Runs orchestrator on a 15-minute cadence.
- Logs errors, continues operation, and does not crash on provider failure.

### Aspire host + Service defaults

- AppHost composes and runs distributed services.
- ServiceDefaults centralizes telemetry, health checks, client resilience, and discovery.

## Why this split?

- **Maintainability**: each concern is isolated and replaceable.
- **Extensibility**: new providers only implement `IProviderIngestionService`.
- **Operational clarity**: ingestion and API can scale/deploy independently.
