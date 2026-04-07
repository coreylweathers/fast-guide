# Architecture

## Runtime topology (what actually runs)

```text
                                      +---------------------------+
                                      | FastGuide.AppHost         |
                                      | (.NET Aspire orchestrator)|
                                      +------------+--------------+
                                                   |
                          +------------------------+------------------------+
                          |                                                 |
                +---------v---------+                             +---------v---------+
                | FastGuide.Api     |                             | FastGuide.Ingestion|
                | Minimal REST API  |                             | Background worker  |
                | Query/read path   |                             | Ingest/write path  |
                +---------+---------+                             +---------+---------+
                          |                                                 |
                          | reads                                           | calls providers
                          |                                                 |
                          |                                        +--------v---------------------+
                          |                                        | IProviderIngestionService[]  |
                          |                                        | - PlutoIngestionService      |
                          |                                        | - PlexIngestionService       |
                          |                                        | - XumoIngestionService       |
                          |                                        +--------+---------------------+
                          |                                                 |
                          |                                                 | HttpClient + Polly retry
                          |                                                 v
                          |                                    +------------+-------------------+
                          |                                    | External provider endpoints    |
                          |                                    | (Pluto / Plex / Xumo JSON)    |
                          |                                    +------------+-------------------+
                          |                                                 |
                          |                                                 v
                          |                                    +------------+-------------------+
                          |                                    | IngestionOrchestrator          |
                          |                                    | + ChannelNormalizer (Core)     |
                          |                                    +------------+-------------------+
                          |                                                 |
                          +--------------------------+----------------------+ 
                                                     |
                                                     v
                                      +--------------+--------------+
                                      | SQLite (FastGuideDbContext) |
                                      | Channels / ProgramSlots     |
                                      | Provider* raw metadata      |
                                      | IngestionLogs               |
                                      +--------------+--------------+
                                                     |
                                                     v
                                         API DTO projections returned
```

## Source-level layering

- **FastGuide.Core**
  - Domain entities (`Channel`, `ProgramSlot`, `ProviderChannel`, `ProviderProgramSlot`, `IngestionLog`).
  - Contracts (`IProviderIngestionService`, `IChannelNormalizer`).
  - Channel dedupe/canonicalization/fuzzy match logic.

- **FastGuide.Infrastructure**
  - `FastGuideDbContext` and EF mappings/indexes.
  - Provider HTTP clients/parsers.
  - `IngestionOrchestrator` that persists normalized + provider-native records.

- **FastGuide.Api**
  - Public REST query endpoints.
  - DTO shaping from normalized storage.
  - Optional startup ingestion kick.

- **FastGuide.Ingestion**
  - `BackgroundService` loop every 15 minutes.
  - Executes orchestrator safely (logs errors, continues).

- **FastGuide.ServiceDefaults**
  - Shared health endpoints, service discovery, HTTP resilience defaults, OpenTelemetry hooks.

- **FastGuide.AppHost**
  - Local distributed composition and startup via .NET Aspire.

## Data flow summary

1. Ingestion worker triggers on schedule.
2. Provider clients fetch remote JSON with retries.
3. Orchestrator normalizes channels/programs and stores both raw+normalized data.
4. API reads normalized tables and projects DTOs for consumers.

## Important note

This MVP currently uses a **single SQLite database** as the shared persistence boundary for both API reads and ingestion writes.
