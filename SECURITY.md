# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

**Please DO NOT open public issues for security vulnerabilities.** Instead:

1. Email security concerns to: **[security contact]** (configure this)
2. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

3. We will:
   - Acknowledge receipt within 48 hours
   - Provide updates every 7 days
   - Issue a CVE when appropriate
   - Credit you (if desired) in the fix release

## Security Best Practices

### Dependencies
- Pinned minimum versions in `.csproj` files to prevent unexpected breaking changes
- Packages checked against known CVEs via GitHub Dependabot
- `packages.lock.json` file locked in version control for reproducible builds

### Infrastructure
- Docker images run as non-root user (UID 1001)
- Security headers applied to all HTTP responses:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy: geolocation=(), microphone=(), camera=()`
- CORS policy restricts cross-origin requests
- Rate limiting (100 requests per 60 seconds) prevents brute force attacks
- Health checks exposed on `/health` endpoint

### Data
- All database queries use parameterized queries (EF Core protection)
- Connection strings stored in `appsettings.json` and environment variables, never committed
- Database migrations run at startup with proper error handling

### Code
- Nullable reference types enabled for null-safety
- Input validation on all public DTOs
- Global exception handler prevents information leakage
- Structured logging for security event tracking

### CI/CD
- Automated builds and tests on every push
- Trivy scans Docker images for vulnerabilities
- Code quality checks via dotnet format
- GitHub Actions runners are ephemeral and isolated

## Container Security

### Base Images
- Official Microsoft images: `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`
- Updated regularly for patches
- Multi-stage builds minimize final image size

### Runtime User
- Container runs as non-root user `appuser` (UID 1001)
- Read-only filesystem recommended in production

### Health Checks
- `HEALTHCHECK` directive validates container health every 30 seconds
- Liveness and readiness probes recommended for Kubernetes

## Compliance

- Aligns with OWASP Top 10 recommendations
- Follows Microsoft Security Best Practices for .NET
- OpenTelemetry integration for security monitoring
- Suitable for HIPAA/SOC2 compliance frameworks (review specific requirements)

## Contact

For security questions, contact: **[Configure security contact email]**

For latest updates, subscribe to: [GitHub Security Advisories](https://github.com/[ORG]/fast-guide/security/advisories)

