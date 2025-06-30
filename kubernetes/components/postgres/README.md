# PostgreSQL Component - Resilient Setup Guide

This PostgreSQL component provides a resilient database setup with automated backups and disaster recovery capabilities using CloudNative-PG.

## Features

- **High Availability**: 2 PostgreSQL instances with automatic failover
- **Automated Backups**: Daily backups to S3-compatible storage
- **Point-in-Time Recovery**: WAL archiving for granular recovery
- **Disaster Recovery**: Bootstrap from backups when needed
- **Monitoring**: Integrated with Prometheus monitoring

## Architecture

```
┌─────────────────┐    ┌─────────────────┐
│  Primary DB     │───▶│  Standby DB     │
│  (Read/Write)   │    │  (Read Only)    │
└─────────────────┘    └─────────────────┘
         │                       │
         ▼                       ▼
┌─────────────────────────────────────────┐
│           S3 Storage                    │
│  ┌─────────────┐  ┌─────────────────┐  │
│  │   Backups   │  │   WAL Archive   │  │
│  └─────────────┘  └─────────────────┘  │
└─────────────────────────────────────────┘
```

## Usage

### Normal Operation (Fresh Deployment)

For a fresh deployment, the cluster will start normally without recovery:

```yaml
# In your app's kustomization.yaml
resources:
  - ../../components/postgres

# In your app's config
patchesStrategicMerge:
  - |
    apiVersion: postgresql.cnpg.io/v1
    kind: Cluster
    metadata:
      name: ${APP}-postgres
    spec:
      # Normal operation - no bootstrap section needed
```

### Disaster Recovery

When you need to restore from a backup due to data loss:

1. **Delete the existing cluster** (if it exists and is corrupted):
   ```bash
   kubectl delete cluster ${APP}-postgres -n ${NAMESPACE}
   kubectl delete pvc ${APP}-postgres-1 ${APP}-postgres-2 -n ${NAMESPACE}  # Only if PVCs are corrupted
   ```

2. **Enable bootstrap recovery**:
   ```yaml
   # In your app's kustomization.yaml - add this patch
   patchesStrategicMerge:
     - |
       apiVersion: postgresql.cnpg.io/v1
       kind: Cluster
       metadata:
         name: ${APP}-postgres
       spec:
         bootstrap:
           recovery:
             source: ${APP}-backup
             database: app
             owner: app
   ```

3. **Apply the configuration**:
   ```bash
   kubectl apply -k .
   ```

4. **Monitor the recovery**:
   ```bash
   kubectl get cluster ${APP}-postgres -n ${NAMESPACE} -w
   kubectl logs -f ${APP}-postgres-1-full-recovery -n ${NAMESPACE}
   ```

5. **Remove bootstrap config after successful recovery**:
   Once the cluster is running, remove the bootstrap section from your patches to prevent issues with future updates.

## Configuration Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `APP` | - | Application name (required) |
| `POSTGRES_STORAGE` | `10Gi` | Storage size for PostgreSQL data |
| `POSTGRES_STORAGE_CLASS` | `longhorn` | Storage class to use |
| `POSTGRES_BACKUP_SCHEDULE` | `0 0 2 * * *` | Cron schedule for backups (daily at 2 AM) |
| `POSTGRES_DATABASE` | `app` | Database name for recovery |
| `POSTGRES_USER` | `app` | Database user for recovery |

## Backup Strategy

### Automated Backups
- **Full backups**: Daily at 2 AM (configurable via `POSTGRES_BACKUP_SCHEDULE`)
- **WAL archiving**: Continuous, 5-minute intervals
- **Storage**: S3-compatible object storage (MinIO)
- **Compression**: gzip compression for both data and WAL files

### Backup Retention
Retention is managed by your S3 bucket lifecycle policies. Recommended settings:
- **Full backups**: 30 days
- **WAL files**: 7 days (minimum for point-in-time recovery)

## Monitoring and Troubleshooting

### Check Cluster Status
```bash
kubectl get cluster ${APP}-postgres -n ${NAMESPACE}
kubectl describe cluster ${APP}-postgres -n ${NAMESPACE}
```

### Check Backup Status
```bash
kubectl get scheduledbackups -n ${NAMESPACE}
kubectl get backups -n ${NAMESPACE}
```

### Check Logs
```bash
# Primary instance logs
kubectl logs ${APP}-postgres-1 -n ${NAMESPACE}

# Backup job logs
kubectl logs -l cnpg.io/jobRole=full-backup -n ${NAMESPACE}

# Recovery job logs (during disaster recovery)
kubectl logs -l cnpg.io/jobRole=full-recovery -n ${NAMESPACE}
```

### Common Issues and Solutions

#### Issue: "Expected empty archive" during recovery
This happens when trying to restore while the WAL archive contains data from a different timeline.

**Solution**:
1. Clean the WAL archive directory in S3 (keep backups)
2. Or use a fresh S3 path for the restored cluster
3. Ensure the backup you're restoring from matches the WAL archive

#### Issue: Recovery job fails with authentication errors
**Solution**: Verify the S3 credentials and bucket permissions:
```bash
kubectl get secret ${APP}-minio-access-key -n ${NAMESPACE} -o yaml
```

#### Issue: Slow recovery performance
**Solution**:
- Increase the `jobs` parameter in the barman configuration
- Ensure sufficient CPU/memory resources
- Check network bandwidth to S3 storage

## Security Best Practices

1. **Encryption**: All backups are encrypted in transit (HTTPS) and at rest (S3)
2. **Access Control**: Use dedicated S3 credentials with minimal permissions
3. **Network Security**: Ensure S3 endpoint is properly secured
4. **Secret Management**: Store credentials in Kubernetes secrets with proper RBAC

## Performance Optimization

### Resource Allocation
- **CPU**: Minimum 50m, up to 1 CPU per instance
- **Memory**: 256Mi minimum, 1Gi maximum recommended
- **Storage**: Use fast storage classes (NVMe SSD recommended)

### PostgreSQL Tuning
The configuration includes optimized settings for:
- Connection pooling (`max_connections: 300`)
- Shared buffers (256MB)
- WAL settings for performance and safety
- Query statistics collection

## Migration from Existing PostgreSQL

To migrate from an existing PostgreSQL setup:

1. **Create a backup** of your existing database
2. **Deploy this component** with recovery disabled
3. **Restore your data** using `pg_dump`/`pg_restore`
4. **Enable automated backups** by deploying the ScheduledBackup

## Advanced Configuration

### Custom Backup Schedules
```yaml
# Multiple backup schedules
---
apiVersion: postgresql.cnpg.io/v1
kind: ScheduledBackup
metadata:
  name: ${APP}-hourly
spec:
  cluster:
    name: ${APP}-postgres
  schedule: "0 0 * * * *"  # Hourly
  immediate: false
  target: prefer-standby
  # ... other settings
```

### Point-in-Time Recovery
```yaml
# Recover to specific timestamp
spec:
  bootstrap:
    recovery:
      source: ${APP}-backup
      recoveryTarget:
        targetTime: "2025-06-29 12:00:00.000000+00"
```

### Read Replicas
```yaml
# Additional read-only replicas
spec:
  instances: 3  # 1 primary + 2 standbys
  replica:
    enabled: true
    source: ${APP}-backup
```

## Support and Documentation

- [CloudNative-PG Documentation](https://cloudnative-pg.io/documentation/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Barman Cloud Plugin](https://github.com/cloudnative-pg/plugin-barman-cloud)
