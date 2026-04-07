# FastGuide Package Lock File Policy

## Overview

FastGuide uses NuGet package lock files (`packages.lock.json`) to ensure reproducible builds across different environments and CI/CD pipelines.

## What is packages.lock.json?

- **Purpose**: Records exact resolved versions of all transitive dependencies
- **Location**: Root level and project directories
- **Auto-Generated**: Created by `dotnet restore` when `RestorePackagesWithLockFile=true`
- **Version Control**: ✅ COMMITTED to version control

## Benefits

### Reproducibility
- Same exact package versions in dev, staging, production
- Eliminates "works on my machine" issues
- Consistent builds across team members

### Security
- Detected compromised packages immediately
- Controlled upgrade cadence
- Audit trail of dependency changes

### Performance
- Faster restores when lock file exists
- No re-resolution of dependency graph

## Workflow

### Initial Setup
```bash
# Clone repository
git clone https://github.com/[ORG]/fast-guide.git
cd fast-guide

# Restore dependencies (creates lock files)
dotnet restore
```

### Updating Dependencies

#### Option 1: Update Specific Package
```bash
# Add or update a package
dotnet add src/FastGuide.Api package Serilog.Sinks.File

# Restore to update lock files
dotnet restore
```

#### Option 2: Interactive Update
```bash
# Review and update packages
dotnet package update --interactive

# Restore
dotnet restore
```

#### Option 3: Commit Lock Files
```bash
# After any package change
dotnet restore

# Commit changes
git add "*.lock.json"
git commit -m "chore: update package lock files"
```

### Reviewing Changes

```bash
# See what changed in lock files
git diff packages.lock.json

# Example output shows version updates
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: Restore dependencies
  run: dotnet restore --locked-mode
```

The `--locked-mode` flag ensures:
- Only versions in lock files are used
- Build fails if lock file is out of date
- No unexpected package updates

## Best Practices

### ✅ DO

- Commit `packages.lock.json` files to version control
- Review lock file changes in PRs
- Use `--locked-mode` in CI/CD
- Update packages regularly (weekly)
- Test after package updates

### ❌ DON'T

- Ignore lock file changes
- Use floating versions (`Version="*"`)
- Manually edit lock files
- Skip testing after updates
- Use `--no-restore` with outdated locks

## Managing Updates

### Security Updates

```bash
# Check for vulnerabilities
dotnet nuget locals all --clear
dotnet restore

# Install security updates immediately
dotnet package update --interactive

# Test and commit
dotnet test
git add packages.lock.json
git commit -m "security: update vulnerable packages"
```

### Regular Updates

```bash
# Monthly or quarterly dependency updates
dotnet package update --interactive

# Run full test suite
dotnet test

# Create PR for team review
git push origin chore/dependency-update
```

## Troubleshooting

### Lock File Out of Sync

```bash
# Delete and regenerate lock files
dotnet restore

# Verify consistency
git status packages.lock.json
```

### Build Fails in CI with Locked Mode

```
error: The lock file at /src/packages.lock.json needs to be updated.
```

**Solution**: Regenerate lock files locally

```bash
dotnet restore
git add packages.lock.json
git commit -m "chore: regenerate package locks"
git push
```

### Merge Conflicts in Lock Files

```bash
# Accept incoming changes (most recent)
git checkout --theirs packages.lock.json

# Restore and regenerate
dotnet restore

# Commit resolution
git add packages.lock.json
git commit -m "chore: resolve lock file conflict"
```

## Monitoring & Alerts

### GitHub Dependabot

- ✅ Automatic PR creation for updates
- ✅ Security alerts for vulnerabilities
- ✅ Automated testing of changes
- Configure in `.github/dependabot.yml`

### Manual Review Process

1. Review dependency changes in lock file
2. Check changelog for breaking changes
3. Run full test suite
4. Test in staging environment
5. Merge and deploy

## Reference

- [Microsoft Package Lock Files](https://learn.microsoft.com/en-us/dotnet/fundamentals/package-management/dependency-resolution#lock-files)
- [NuGet Restore Behavior](https://learn.microsoft.com/en-us/nuget/consume-packages/package-restore)
- [GitHub Dependabot](https://docs.github.com/en/code-security/dependabot)

