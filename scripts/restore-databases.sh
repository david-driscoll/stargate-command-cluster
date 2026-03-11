#!/usr/bin/env zsh
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

BACKUP_DIR="/Volumes/backup/postgres/temp"
APP_NAMESPACE="equestria"
POSTGRES_CONNECTIONSTRING=$(op read "op://Eris/equestria-postgres-superuser/public-uri")

DATABASES=("pulsarr" "questarr")

# Track original replica counts
typeset -A ORIGINAL_REPLICAS

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Database Restore Script               ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# Validate local tooling and DB connectivity
if ! command -v psql >/dev/null 2>&1; then
    echo -e "${RED}Error: psql is not installed or not in PATH${NC}"
    exit 1
fi
if ! command -v pg_restore >/dev/null 2>&1; then
    echo -e "${RED}Error: pg_restore is not installed or not in PATH${NC}"
    exit 1
fi

echo -e "${YELLOW}Validating PostgreSQL connection...${NC}"
if ! psql "$POSTGRES_CONNECTIONSTRING" -v ON_ERROR_STOP=1 -Atqc "SELECT 1;" >/dev/null 2>&1; then
    echo -e "${RED}Error: Unable to connect using POSTGRES_CONNECTIONSTRING${NC}"
    exit 1
fi
echo -e "${GREEN}✓ PostgreSQL connection is valid${NC}"
echo ""

build_conn_for_db() {
    local db_name="$1"
    local without_query host_and_db query
    without_query="${POSTGRES_CONNECTIONSTRING%%\?*}"
    host_and_db="${without_query%/*}"

    if [[ "$POSTGRES_CONNECTIONSTRING" == *\?* ]]; then
        query="${POSTGRES_CONNECTIONSTRING#*\?}"
        echo "${host_and_db}/${db_name}?${query}"
    else
        echo "${host_and_db}/${db_name}"
    fi
}

# Scale down deployments
echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Scaling Down Applications             ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

for DB in "${DATABASES[@]}"; do
    echo -e "${YELLOW}Scaling down: $DB${NC}"

    # Try to find HelmRelease first
    HR_EXISTS=$(kubectl get helmrelease -n $APP_NAMESPACE $DB 2>/dev/null || echo "")
    if [ -n "$HR_EXISTS" ]; then
        # Get original replica count from deployment before suspending
        REPLICAS=$(kubectl get deployment -n $APP_NAMESPACE -l app.kubernetes.io/name=$DB -o jsonpath='{.items[0].spec.replicas}' 2>/dev/null || echo "1")
        ORIGINAL_REPLICAS[$DB]=$REPLICAS
        echo "  Stored replica count: $REPLICAS"

        # Suspend HelmRelease
        echo "  Suspending HelmRelease..."
        kubectl patch helmrelease -n $APP_NAMESPACE $DB --type=merge -p '{"spec":{"suspend":true}}' 2>/dev/null || true

        # Scale down deployment
        echo "  Scaling deployment to 0..."
        kubectl scale deployment -n $APP_NAMESPACE -l app.kubernetes.io/name=$DB --replicas=0 2>/dev/null || true

        # Wait for pods to terminate
        # kubectl wait --for=delete pod -n $APP_NAMESPACE -l app.kubernetes.io/name=$DB --timeout=60s 2>/dev/null || true
        # echo -e "${GREEN}  ✓ Scaled down${NC}"
    else
        echo -e "${YELLOW}  ⚠ HelmRelease not found, skipping scale down${NC}"
        ORIGINAL_REPLICAS[$DB]=1
    fi
    echo ""
done

# Restore databases
echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Restoring Databases                   ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

RESTORED=0
FAILED=0

for DB in "${DATABASES[@]}"; do
    echo -e "${YELLOW}Processing: $DB${NC}"

    # Find latest backup file
    LATEST_BACKUP=$(ls -1t "$BACKUP_DIR/$DB.sql.gz" 2>/dev/null | head -1)

    if [ -z "$LATEST_BACKUP" ]; then
        echo -e "${RED}  ✗ No backup found for $DB${NC}"
        FAILED=$((FAILED + 1))
        echo ""
        continue
    fi

    echo "  Backup: $(basename $LATEST_BACKUP)"
    BACKUP_SIZE=$(du -h "$LATEST_BACKUP" | cut -f1)
    echo "  Size: $BACKUP_SIZE"

    # Check if database exists
    DB_EXISTS=$(psql "$POSTGRES_CONNECTIONSTRING" -Atqc "SELECT 1 FROM pg_database WHERE datname = '$DB';" 2>/dev/null || true)

    if [ -n "$DB_EXISTS" ]; then
        echo "  Dropping existing database..."
        psql "$POSTGRES_CONNECTIONSTRING" -v ON_ERROR_STOP=1 -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DB' AND pid <> pg_backend_pid();" >/dev/null 2>&1 || true
        psql "$POSTGRES_CONNECTIONSTRING" -v ON_ERROR_STOP=1 -c "DROP DATABASE \"$DB\";" >/dev/null 2>&1 || true
    fi

    # Create owner role if it doesn't exist
    echo "  Ensuring owner role exists..."
    psql "$POSTGRES_CONNECTIONSTRING" -v ON_ERROR_STOP=1 -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$DB') THEN CREATE ROLE \"$DB\" WITH LOGIN PASSWORD NULL; END IF; END \$\$;" >/dev/null 2>&1 || {
        echo -e "${RED}  ✗ Failed to create owner role $DB${NC}"
        FAILED=$((FAILED + 1))
        echo ""
        continue
    }

    # Create database
    echo "  Creating database..."
    psql "$POSTGRES_CONNECTIONSTRING" -v ON_ERROR_STOP=1 -c "CREATE DATABASE \"$DB\" OWNER \"$DB\";" >/dev/null 2>&1 || {
        echo -e "${RED}  ✗ Failed to create database $DB${NC}"
        FAILED=$((FAILED + 1))
        echo ""
        continue
    }

    # Restore from backup
    echo "  Restoring from backup (this may take a while)..."
    DB_CONNECTIONSTRING=$(build_conn_for_db "$DB")
    BACKUP_MAGIC=$(gunzip -c "$LATEST_BACKUP" 2>/dev/null | head -c 5 || true)
    LOG_FILE="/tmp/restore-${DB}.log"
    RESTORE_SUCCESS=0

    if [ "$BACKUP_MAGIC" = "PGDMP" ]; then
        # Backup produced by pg_dump --format=custom; restore with pg_restore.
        if gunzip -c "$LATEST_BACKUP" | pg_restore --dbname="$DB_CONNECTIONSTRING" --no-owner --no-privileges --exit-on-error --verbose >"$LOG_FILE" 2>&1; then
            echo -e "${GREEN}  ✓ Successfully restored $DB (custom format)${NC}"
            RESTORED=$((RESTORED + 1))
            RESTORE_SUCCESS=1
        else
            echo -e "${RED}  ✗ Failed to restore $DB${NC}"
            echo "  Log: $LOG_FILE"
            tail -n 20 "$LOG_FILE" || true
            FAILED=$((FAILED + 1))
        fi
    elif gunzip -c "$LATEST_BACKUP" | psql "$DB_CONNECTIONSTRING" -v ON_ERROR_STOP=1 >"$LOG_FILE" 2>&1; then
        echo -e "${GREEN}  ✓ Successfully restored $DB${NC}"
        RESTORED=$((RESTORED + 1))
        RESTORE_SUCCESS=1
    else
        echo -e "${RED}  ✗ Failed to restore $DB${NC}"
        echo "  Log: $LOG_FILE"
        tail -n 20 "$LOG_FILE" || true
        FAILED=$((FAILED + 1))
    fi

    if [ $RESTORE_SUCCESS -eq 1 ]; then
        echo "  Setting database ownership..."
        psql "$DB_CONNECTIONSTRING" -v ON_ERROR_STOP=1 <<-EOSQL >/dev/null 2>&1 || true
            -- Grant all privileges on database
            GRANT ALL PRIVILEGES ON DATABASE "$DB" TO "$DB";

            -- Reassign table ownership
            DO \$\$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN
                    SELECT tablename FROM pg_tables
                    WHERE schemaname = 'public'
                LOOP
                    EXECUTE 'ALTER TABLE public.' || quote_ident(r.tablename) || ' OWNER TO "' || '$DB' || '"';
                END LOOP;
            END \$\$;

            -- Reassign sequence ownership
            DO \$\$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN
                    SELECT sequencename FROM pg_sequences
                    WHERE schemaname = 'public'
                LOOP
                    EXECUTE 'ALTER SEQUENCE public.' || quote_ident(r.sequencename) || ' OWNER TO "' || '$DB' || '"';
                END LOOP;
            END \$\$;

            -- Grant privileges on all tables in public schema
            GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO "$DB";
            GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO "$DB";
            GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO "$DB";
EOSQL
        echo -e "${GREEN}  ✓ Ownership configured${NC}"
    fi

    echo ""
done

# Scale up deployments
echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Scaling Up Applications               ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

for DB in "${DATABASES[@]}"; do
    echo -e "${YELLOW}Scaling up: $DB${NC}"

    REPLICAS=${ORIGINAL_REPLICAS[$DB]:-1}
    echo "  Target replicas: $REPLICAS"

    # Resume HelmRelease
    HR_EXISTS=$(kubectl get helmrelease -n $APP_NAMESPACE $DB 2>/dev/null || echo "")
    if [ -n "$HR_EXISTS" ]; then
        echo "  Resuming HelmRelease..."
        kubectl patch helmrelease -n $APP_NAMESPACE $DB --type=merge -p '{"spec":{"suspend":false}}' 2>/dev/null || true

        # Give Flux a moment to reconcile
        sleep 2

        # Scale deployment back up
        echo "  Scaling deployment to $REPLICAS..."
        kubectl scale deployment -n $APP_NAMESPACE -l app.kubernetes.io/name=$DB --replicas=$REPLICAS 2>/dev/null || true

        echo -e "${GREEN}  ✓ Scaled up${NC}"
    else
        echo -e "${YELLOW}  ⚠ HelmRelease not found, skipping scale up${NC}"
    fi
    echo ""
done

# Summary
echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Restore Summary                       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}Restored: $RESTORED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

# Verify databases
echo -e "${YELLOW}Verifying databases:${NC}"
psql "$POSTGRES_CONNECTIONSTRING" -Atqc "SELECT datname FROM pg_database WHERE datname IN ('immich','n8n','pulsarr','questarr','retrom','romm','tandoor','vikunja') ORDER BY datname;" || true
echo ""

# Check application status
echo -e "${YELLOW}Application status:${NC}"
kubectl get pods -n $APP_NAMESPACE -l app.kubernetes.io/name -o wide 2>/dev/null | grep -E "immich|n8n|pulsarr|questarr|retrom|romm|tandoor|vikunja" || echo "No pods found (they may be starting up)"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All databases restored successfully!${NC}"
    echo -e "${YELLOW}Note: Applications may take a few minutes to fully start up${NC}"
    exit 0
else
    echo -e "${RED}✗ Some databases failed to restore${NC}"
    exit 1
fi
