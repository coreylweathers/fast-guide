# Ingestion pipeline

## Providers currently wired

- Pluto
- Plex
- Xumo

## End-to-end flow

1. Worker ticks every 15 minutes.
2. `IngestionOrchestrator` invokes each provider service.
3. Provider service fetches JSON with Polly retries + exponential backoff.
4. Provider payload is converted into provider-neutral records.
5. Normalizer resolves channels by:
   - canonical exact match
   - fuzzy match threshold
   - provider priority fallback
6. Orchestrator stores:
   - raw provider channel/program data
   - normalized `Channel` and `ProgramSlot`
   - ingestion logs for success/failure

## Error handling principles

- Provider-specific failures are logged and isolated.
- One provider failure does not block others.
- Invalid JSON and HTTP failures are handled gracefully.

## Adding a new provider

1. Create new class implementing `IProviderIngestionService`.
2. Register typed `HttpClient` + interface mapping in `AddFastGuideInfrastructure`.
3. Parse provider payload to `ProviderChannelPayload` and `ProviderProgramPayload`.
4. Add focused parser unit tests.
