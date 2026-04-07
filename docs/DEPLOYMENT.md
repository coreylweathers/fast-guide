# Deployment Guide

## Overview

This guide covers deploying FastGuide to various environments: Development, Staging, and Production.

## Prerequisites

- Docker and Docker Compose
- .NET 10 SDK (for local development)
- Access to target environment
- Environment-specific configuration files

## Environment Configurations

### Development

**Purpose**: Local testing and development  
**Database**: SQLite (local file)  
**Logging**: Debug level  
**Security**: CORS allows localhost

```bash
dotnet run --project src/FastGuide.Api/FastGuide.Api.csproj
# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/api-docs
```

### Staging

**Purpose**: Pre-production testing  
**Database**: SQLite (in container volume)  
**Logging**: Information level  
**Security**: CORS restricted to staging domain

```bash
docker compose -f docker-compose.staging.yml up -d
```

### Production

**Purpose**: Live environment  
**Database**: SQL Server or PostgreSQL (recommended)  
**Logging**: Warning level  
**Security**: CORS restricted to production domain only

See "Production Deployment" section below.

## Docker Deployment

### Build Image

```bash
# Build locally
docker build -t fastguide:latest .

# Build with specific version
docker build -t fastguide:1.0.0 .

# Build for multiple platforms (requires buildx)
docker buildx build --platform linux/amd64,linux/arm64 -t fastguide:latest .
```

### Docker Compose (Development/Staging)

```bash
# Start services
docker compose up -d

# View logs
docker compose logs -f fastguide-api

# Stop services
docker compose down

# Stop and remove volumes
docker compose down -v
```

### Container Registry

#### Push to GitHub Container Registry (GHCR)

```bash
# Login to GHCR
echo ${{ secrets.GITHUB_TOKEN }} | docker login ghcr.io -u ${{ github.actor }} --password-stdin

# Tag image
docker tag fastguide:latest ghcr.io/[ORG]/fast-guide:latest
docker tag fastguide:latest ghcr.io/[ORG]/fast-guide:1.0.0

# Push
docker push ghcr.io/[ORG]/fast-guide:latest
docker push ghcr.io/[ORG]/fast-guide:1.0.0
```

## Kubernetes Deployment (Optional)

### Helm Chart Structure

```yaml
# values.yaml
image:
  repository: ghcr.io/[ORG]/fast-guide
  tag: 1.0.0
  pullPolicy: IfNotPresent

replicas: 2

resources:
  requests:
    memory: "256Mi"
    cpu: "100m"
  limits:
    memory: "512Mi"
    cpu: "500m"

service:
  type: LoadBalancer
  port: 80
  targetPort: 8080

ingress:
  enabled: true
  hosts:
    - host: api.example.com
      paths:
        - path: /

database:
  type: postgresql
  host: postgres-service
  name: fastguide_prod
```

### Deploy with Helm

```bash
# Install
helm install fastguide ./chart -f values.yaml

# Upgrade
helm upgrade fastguide ./chart -f values.yaml

# Uninstall
helm uninstall fastguide
```

## Production Deployment

### Pre-Deployment Checklist

- [ ] All tests pass locally
- [ ] Security scanning complete (Trivy)
- [ ] Secrets configured (not in image)
- [ ] Database backups scheduled
- [ ] Monitoring configured
- [ ] Alerting configured
- [ ] Rollback plan documented

### Configuration

Create `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "FastGuide": "Information"
    }
  },
  "ConnectionStrings": {
    "FastGuide": "Server=[PROD_SERVER];Database=FastGuide;Trusted_Connection=true;"
  },
  "AllowedHosts": "api.example.com",
  "Cors": {
    "AllowedOrigins": ["https://example.com"]
  }
}
```

### Environment Variables

```bash
# Security
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTPS_PORT=443

# Database
ConnectionStrings__FastGuide=Server=[PROD_DB];Database=FastGuide;...

# OpenTelemetry
OTEL_EXPORTER_OTLP_ENDPOINT=https://[OTEL_COLLECTOR]:4317

# Logging
Serilog__MinimumLevel=Warning

# Feature flags
StartupIngestionEnabled=false
```

### Database Migration

```bash
# Run migrations at startup (automatic via code)
# Or manual:
dotnet ef database update --configuration Release --project src/FastGuide.Infrastructure

# Backup before migration
# sqlcmd -S [SERVER] -Q "BACKUP DATABASE FastGuide TO DISK='D:\Backups\fastguide_$(date).bak'"
```

### SSL/TLS Configuration

```dockerfile
# In production Dockerfile
ENV ASPNETCORE_URLS=https://+:443;http://+:80
ENV ASPNETCORE_HTTPS_PORT=443
COPY ./certs/cert.pfx /app/cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=$CERT_PASSWORD
```

### Performance Tuning

```csharp
// Program.cs - Production optimizations
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCaching();
    app.UseHttpCacheHeaders();
    
    // Increase connection pool
    services.AddDbContextPool<FastGuideDbContext>(
        options => options.UseSqlServer(...),
        poolSize: 128);
}
```

### Monitoring & Logging

#### Application Insights (Azure)

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

#### Prometheus Metrics

```csharp
builder.Services.AddPrometheusAspNetCoreMetrics();
app.UsePrometheusMetrics();
```

### Backup & Recovery

#### Database Backup

```bash
# Daily backup schedule
0 2 * * * /scripts/backup-db.sh >> /var/log/backup.log 2>&1

# Backup script
#!/bin/bash
BACKUP_FILE="/backups/fastguide_$(date +%Y%m%d_%H%M%S).bak"
sqlcmd -S [SERVER] -Q "BACKUP DATABASE FastGuide TO DISK='$BACKUP_FILE'"
```

#### Data Recovery

```bash
# Restore from backup
sqlcmd -S [SERVER] -Q "RESTORE DATABASE FastGuide FROM DISK='[BACKUP_FILE]'"
```

## Health Checks & Monitoring

### Health Endpoint

```bash
# Check service health
curl https://api.example.com/health

# Response
{
  "status": "healthy"
}
```

### Liveness & Readiness Probes (Kubernetes)

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

## Rollback Procedure

### Docker

```bash
# Rollback to previous image
docker service update --image fastguide:1.0.0 fastguide-api

# Or with docker-compose
docker compose down
git checkout HEAD~1 docker-compose.yml
docker compose up -d
```

### Kubernetes

```bash
# View rollout history
kubectl rollout history deployment/fastguide

# Rollback to previous version
kubectl rollout undo deployment/fastguide

# Rollback to specific revision
kubectl rollout undo deployment/fastguide --to-revision=2
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker logs [CONTAINER_ID]

# Common issues:
# 1. Port already in use - change port mapping
# 2. Database connection failed - verify connection string
# 3. Permission denied - check file permissions
```

### High Memory Usage

```bash
# Check memory limits
docker stats [CONTAINER_ID]

# Adjust in docker-compose.yml
deploy:
  resources:
    limits:
      memory: 512M
    reservations:
      memory: 256M
```

### Slow API Response

1. Check database query performance
2. Verify rate limiting isn't blocking requests
3. Review application logs for errors
4. Check CPU/memory utilization

## Post-Deployment

1. **Smoke Tests**: Verify core functionality
   ```bash
   curl https://api.example.com/health
   curl https://api.example.com/api/v1/channels
   ```

2. **Monitoring**: Check application metrics and logs

3. **Notifications**: Alert team of successful deployment

4. **Documentation**: Update deployment date and version

## Support & Escalation

- **Issues**: Check application logs in `/var/log/` or CloudWatch
- **Escalation**: Contact DevOps/SRE team
- **Emergency**: See on-call procedures in SECURITY.md

## References

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/)

