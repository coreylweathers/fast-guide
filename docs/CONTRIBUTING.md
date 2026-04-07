# Contributing

## Development workflow

1. Run via Aspire AppHost for integrated local execution.
2. Keep domain logic in `FastGuide.Core`.
3. Keep transport/persistence/provider logic in `FastGuide.Infrastructure`.
4. Keep API focused on query endpoints and DTO composition.
5. Keep ingestion scheduling in `FastGuide.Ingestion`.

## Coding guidelines

- Favor explicit code over hidden magic.
- Add/extend tests for behavior changes.
- Document provider parsing assumptions in `docs/INGESTION.md`.

## Test strategy

- Unit tests for normalization and parsing.
- Integration tests for API endpoints with in-memory DB.

## Suggested commands

```bash
dotnet restore FastGuide.sln
dotnet build FastGuide.sln
dotnet test tests/FastGuide.Tests/FastGuide.Tests.csproj
```
