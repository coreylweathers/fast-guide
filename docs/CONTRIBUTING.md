# Contributing

## Welcome! 👋

Thank you for your interest in contributing to FastGuide! This guide will help you get started.

## Development Setup

### Prerequisites
- .NET 10 SDK
- Git
- Docker (for testing containerized builds)
- A code editor (Visual Studio, Rider, VS Code)

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/[ORG]/fast-guide.git
cd fast-guide

# Restore dependencies
dotnet restore FastGuide.sln

# Build the solution
dotnet build FastGuide.sln

# Run tests
dotnet test tests/FastGuide.Tests/FastGuide.Tests.csproj

# Run with Aspire
dotnet run --project src/FastGuide.AppHost/FastGuide.AppHost.csproj
```

## Development Workflow

### Architecture Principles

1. **Domain Logic** → Keep in `FastGuide.Core`
   - Models, interfaces, normalization logic
   - No external dependencies

2. **Infrastructure** → Keep in `FastGuide.Infrastructure`
   - Database access (EF Core)
   - External API clients
   - Ingestion orchestration

3. **API Layer** → Keep in `FastGuide.Api`
   - HTTP endpoints
   - DTOs (data transfer objects)
   - HTTP-specific concerns

4. **Background Jobs** → Keep in `FastGuide.Ingestion`
   - Scheduled tasks
   - Worker registration

5. **Service Defaults** → Keep in `FastGuide.ServiceDefaults`
   - Cross-cutting concerns
   - Health checks
   - OpenTelemetry

### Making Changes

#### 1. Create a Feature Branch
```bash
git checkout -b feature/your-feature-name
```

#### 2. Code Quality Checks

```bash
# Format code
dotnet format

# Build solution
dotnet build FastGuide.sln

# Run all tests
dotnet test tests/FastGuide.Tests/FastGuide.Tests.csproj
```

#### 3. Commit Your Changes

Use clear commit messages:
```bash
git commit -m "feat: add rate limiting to API"
git commit -m "fix: handle null channel names"
git commit -m "docs: update guidelines"
```

#### 4. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

## Testing Guidelines

### Unit Tests
- Location: `tests/FastGuide.Tests/`
- Framework: xUnit
- Pattern: AAA (Arrange, Act, Assert)
- Coverage goal: ≥ 80%

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~NormalizationTests"
```

## Code Quality Standards

- ✅ Pass `dotnet format` verification
- ✅ Nullable reference types enabled
- ✅ XML documentation for public members
- ✅ No hardcoded secrets
- ✅ All tests pass

See [docs/CODE_QUALITY.md](CODE_QUALITY.md) for detailed guidelines.

## Coding Guidelines

- Favor explicit code over hidden magic
- Add/extend tests for behavior changes
- Document provider parsing assumptions in `docs/INGESTION.md`
- Use pattern matching over `is`/`as`
- Use null-conditional operators (`?.`)

## Security & Best Practices

### When Adding New Features
- [ ] Add input validation
- [ ] Use parameterized queries (EF Core)
- [ ] Add appropriate logging
- [ ] Consider performance
- [ ] Add tests for error cases
- [ ] No hardcoded secrets

See [SECURITY.md](../SECURITY.md) for complete security guidelines.

## Common Tasks

### Adding a New API Endpoint

1. Create DTO in `FastGuide.Api/Dtos/`
2. Add validation attributes
3. Create endpoint in `Program.cs`
4. Add tests in `FastGuide.Tests/`
5. Update `docs/API.md`

### Adding a New Provider

1. Create provider implementation in `FastGuide.Infrastructure/Providers/`
2. Implement `IProviderIngestionService`
3. Register in `ServiceCollectionExtensions.cs`
4. Add normalization logic in `FastGuide.Core/Normalization/`
5. Add tests for parsing

## Useful Commands

```bash
# Development workflow
dotnet build              # Build solution
dotnet test              # Run tests
dotnet format            # Format code

# Aspire
dotnet run --project src/FastGuide.AppHost/FastGuide.AppHost.csproj

# Docker
docker build -t fastguide:latest .
docker compose up --build
```

## Code Review Process

### Reviewers Will Check For:
- ✅ Follows coding standards
- ✅ Tests are comprehensive
- ✅ Documentation is complete
- ✅ No security concerns
- ✅ Performance implications considered

### Tips for Quick Approvals:
- Keep PRs focused and reasonably sized
- Add clear descriptions
- Link to related issues
- Respond promptly to feedback

## License

By contributing, you agree to license your contributions under the same license as the project.

