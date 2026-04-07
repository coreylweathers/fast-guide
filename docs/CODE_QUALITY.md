# Code Quality Guidelines

This document outlines the code quality standards and practices for the FastGuide project.

## Overview

FastGuide implements comprehensive code quality measures aligned with Microsoft .NET best practices and industry standards.

## Automated Quality Checks

### 1. EditorConfig
- **Purpose**: Enforce consistent code style across the solution
- **File**: `.editorconfig`
- **Coverage**: C#, JSON, XML, YAML, Markdown

Key settings:
```ini
[*.cs]
# Enforces PascalCase for methods/properties
# Enforces camelCase for local variables
# Enforces camelCase with underscore prefix for private fields
# Null-conditional operators required
# Pattern matching over is/as expressions
```

### 2. Nullable Reference Types
- **Enabled**: ✅ `<Nullable>enable</Nullable>`
- **Purpose**: Prevent null reference exceptions at compile time
- **Impact**: All reference types are non-nullable by default unless marked with `?`

Example:
```csharp
// Compile error: cannot assign null to non-nullable string
string name = null;

// Correct: nullable string
string? optionalName = null;
```

### 3. Implicit Usings
- **Enabled**: ✅ `<ImplicitUsings>enable</ImplicitUsings>`
- **Purpose**: Reduce boilerplate common imports
- **Namespaces**: System, System.Collections.Generic, System.Linq, System.Threading, etc.

### 4. Format Verification
- **Tool**: `dotnet format` (part of CI/CD)
- **When**: On every commit (local) and PR (CI/CD)
- **Command**: `dotnet format --verify-no-changes`

## Code Style Standards

### Naming Conventions

| Element | Pattern | Example |
|---------|---------|---------|
| Public Methods | PascalCase | `GetChannels()` |
| Public Properties | PascalCase | `Name` |
| Local Variables | camelCase | `channelName` |
| Private Fields | _camelCase | `_channelCache` |
| Constants | UPPER_CASE or PascalCase | `MAX_RETRIES` |
| Namespaces | PascalCase | `FastGuide.Infrastructure` |

### File Organization

```csharp
// 1. Using statements (ordered by namespace)
using System;
using Microsoft.EntityFrameworkCore;

// 2. File-scoped namespace
namespace FastGuide.Api.Dtos;

// 3. Interfaces (if any)
public interface IChannelService { }

// 4. Classes/Records (grouped by access level)
public sealed record ChannelDto(...);
public class ChannelService { }
internal sealed class ChannelNormalizer { }
```

### Expression-Bodied Members

```csharp
// Use for simple single-line properties
public string Name => _name ?? string.Empty;

// Use for simple methods
public Guid Id => _id;

// Don't use for complex logic
public void ProcessChannel()
{
    // Complex logic here
}
```

### Pattern Matching

```csharp
// Good: Pattern matching over is/as
if (entity is ChannelDto channel)
{
    // Use channel directly
}

// Avoid: Old-style casting
if (entity is ChannelDto)
{
    var channel = (ChannelDto)entity;
}

// Good: Switch expressions
var result = entity switch
{
    ChannelDto => "channel",
    ProgramSlotDto => "program",
    _ => "unknown"
};
```

### Null Handling

```csharp
// Good: Null-coalescing operator
var name = channel?.Name ?? "Unknown";

// Good: Null-conditional operator
var channelName = slot?.Channel?.Name;

// Use throw expressions for validation
public ChannelDto(Guid id, string name) =>
    Name = name ?? throw new ArgumentNullException(nameof(name));
```

## Testing Standards

### Test Organization

```
tests/
├── FastGuide.Tests/
    ├── Unit/
    │   ├── NormalizationTests.cs
    │   └── ValidationTests.cs
    ├── Integration/
    │   ├── ApiTests.cs
    │   └── IngestionTests.cs
    └── Fixtures/
        ├── WebApplicationFactory.cs
        └── TestData.cs
```

### xUnit Conventions

```csharp
public class ChannelNormalizationTests
{
    // Arrange, Act, Assert (AAA pattern)
    [Fact]
    public async Task NormalizeChannel_WithValidData_ReturnsCorrectResult()
    {
        // Arrange
        var input = new PlutoChannel { Name = "HBO" };
        var normalizer = new ChannelNormalizer();

        // Act
        var result = await normalizer.NormalizeAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HBO", result.Name);
    }

    // Theory for parameterized tests
    [Theory]
    [InlineData("HBO", "hbo")]
    [InlineData("CNN", "cnn")]
    public void NormalizeChannelName_WithInput_ReturnsLowercase(string input, string expected)
    {
        Assert.Equal(expected, input.ToLower());
    }
}
```

### Test Coverage Goals

- **Target**: ≥ 80% coverage
- **Critical paths**: 100% coverage
- **UI/Web layer**: Smoke tests + integration tests
- **Ignored**: Property getters, generated code

## Documentation Standards

### XML Documentation

```csharp
/// <summary>
/// Gets or normalizes channel data from multiple providers.
/// </summary>
/// <param name="channelData">The raw channel data to normalize</param>
/// <returns>A normalized channel object ready for database storage</returns>
/// <exception cref="ArgumentNullException">Thrown when channelData is null</exception>
public async Task<Channel> NormalizeAsync(IChannelData channelData)
{
    // Implementation
}
```

### Markdown Documentation

- **README.md**: Overview, quick start, key features
- **docs/**: Detailed architecture, ingestion, API docs
- **SECURITY.md**: Security policies and practices
- **CODE_QUALITY.md**: This file
- **CONTRIBUTING.md**: Development workflow, PR guidelines

## Performance Considerations

### Entity Framework Core

```csharp
// Good: AsNoTracking for read-only queries
var channels = await db.Channels
    .AsNoTracking()
    .OrderBy(c => c.Name)
    .ToListAsync();

// Good: Include for eager loading
var slots = await db.ProgramSlots
    .Include(ps => ps.Channel)
    .ToListAsync();

// Good: Projection to minimize data transfer
var dtos = await db.Channels
    .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.Category))
    .ToListAsync();

// Avoid: Loading all columns unnecessarily
var slots = await db.ProgramSlots.ToListAsync();
```

### Async/Await

```csharp
// Good: Async all the way
public async Task<IReadOnlyList<ChannelDto>> GetChannelsAsync()
{
    return await db.Channels
        .ToListAsync();
}

// Avoid: Mixing sync over async (can cause deadlocks)
var channels = db.Channels.ToList(); // Blocks
```

## Security Standards

### Input Validation

```csharp
// All public DTOs have validation
public sealed record SearchDto(
    [StringLength(100, MinimumLength = 1)]
    string Query);

// Validate on entry
if (!ModelState.IsValid)
{
    return BadRequest(ModelState);
}
```

### Parameterized Queries

```csharp
// Good: EF Core uses parameterized queries
var channels = await db.Channels
    .Where(c => EF.Functions.Like(c.Name, $"%{query}%"))
    .ToListAsync();

// Never: String concatenation (SQL injection)
var sql = $"SELECT * FROM Channels WHERE Name LIKE '%{query}%'";
```

### Secrets Management

```csharp
// Good: Configuration-based
var connectionString = configuration.GetConnectionString("FastGuide");

// Avoid: Hardcoded secrets
var connectionString = "Server=...;Password=hardcoded";
```

## Review Checklist

Before submitting a PR, verify:

- [ ] Code compiles without warnings
- [ ] `dotnet format --verify-no-changes` passes
- [ ] All tests pass: `dotnet test`
- [ ] New code has ≥ 80% test coverage
- [ ] XML documentation added for public members
- [ ] No hardcoded secrets
- [ ] No `async void` methods (except event handlers)
- [ ] Proper null handling with `?` and `??`
- [ ] No warnings in Solution Explorer
- [ ] Breaking changes documented

## CI/CD Integration

All code quality checks run automatically:

1. **Local**: Pre-commit hooks (configure husky if desired)
2. **Pull Request**: GitHub Actions runs on every PR
3. **Merge**: Failing checks block merge to main

See `.github/workflows/build-and-test.yml` for details.

## Tools & Commands

### Format Code
```bash
dotnet format
```

### Check Format
```bash
dotnet format --verify-no-changes --verbosity diagnostic
```

### Run Tests with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

### Build and Test
```bash
dotnet build && dotnet test
```

## References

- [Microsoft .NET Code Style](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/)
- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netfx)
- [EditorConfig Specification](https://editorconfig.org/)

