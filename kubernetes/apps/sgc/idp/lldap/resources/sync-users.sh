#!/bin/bash
set -euo pipefail

echo "Starting LLDAP user sync using bootstrap.sh..."

# Environment variables expected by bootstrap.sh
export LLDAP_URL="${LLDAP_URL:-http://lldap.sgc.svc.cluster.local:17170}"
export LLDAP_ADMIN_USERNAME="${LLDAP_LDAP_USER_DN:-admin}"
export LLDAP_ADMIN_PASSWORD="${LLDAP_LDAP_USER_PASS}"
export USER_CONFIGS_DIR="${USER_CONFIGS_DIR:-/shared/user-configs}"
export GROUP_CONFIGS_DIR="${GROUP_CONFIGS_DIR:-/shared/group-configs}"
export DO_CLEANUP="${DO_CLEANUP:-false}"
export LLDAP_SET_PASSWORD_PATH="${LLDAP_SET_PASSWORD_PATH:-/app/lldap_set_password}"

# Additional environment variables that may be needed by bootstrap.sh
export LLDAP_JWT_SECRET="${LLDAP_JWT_SECRET:-}"
export LLDAP_KEY_SEED="${LLDAP_KEY_SEED:-}"
export LLDAP_LDAP_BASE_DN="${LLDAP_LDAP_BASE_DN:-dc=example,dc=com}"
export LLDAP_LDAP_USER_EMAIL="${LLDAP_LDAP_USER_EMAIL:-admin@example.com}"

# Validate required environment variables
if [ -z "$LLDAP_ADMIN_PASSWORD" ]; then
    echo "Error: LLDAP_LDAP_USER_PASS environment variable not set"
    exit 1
fi

if [ -z "$LLDAP_LDAP_BASE_DN" ]; then
    echo "Error: LLDAP_LDAP_BASE_DN environment variable not set"
    exit 1
fi

# Check if bootstrap.sh exists
if [ ! -f "/app/bootstrap.sh" ]; then
    echo "Error: bootstrap.sh not found at /app/bootstrap.sh"
    echo "This script requires the LLDAP bootstrap.sh script to be available"
    exit 1
fi

# Check if lldap_set_password exists if specified
if [ -n "$LLDAP_SET_PASSWORD_PATH" ] && [ ! -f "$LLDAP_SET_PASSWORD_PATH" ]; then
    echo "Warning: lldap_set_password not found at $LLDAP_SET_PASSWORD_PATH"
    echo "Password setting functionality may not work"
fi

# Create required directories
mkdir -p "$USER_CONFIGS_DIR"
mkdir -p "$GROUP_CONFIGS_DIR"

# Create default group configs directory and basic groups if they don't exist

# Create default groups if group configs directory is empty
if [ -z "$(ls -A "$GROUP_CONFIGS_DIR" 2>/dev/null || true)" ]; then
    echo "Creating default group configurations..."

    # Create lldap_password_manager group config
    cat >"$GROUP_CONFIGS_DIR/lldap_password_manager.json" <<'EOF'
{
  "name": "lldap_password_manager"
}
EOF

    echo "Created default group config: lldap_password_manager"
fi

# Check if user configs directory exists and has files
if [ ! -d "$USER_CONFIGS_DIR" ] || [ -z "$(ls -A "$USER_CONFIGS_DIR" 2>/dev/null || true)" ]; then
    echo "No user configs found in $USER_CONFIGS_DIR, nothing to sync"
    exit 0
fi

echo "User configs directory: $USER_CONFIGS_DIR"
echo "Group configs directory: $GROUP_CONFIGS_DIR"
echo "Found $(find "$USER_CONFIGS_DIR" -name "*.json" | wc -l) user config files"
echo "Found $(find "$GROUP_CONFIGS_DIR" -name "*.json" | wc -l) group config files"

# Wait for LLDAP to be ready with timeout
echo "Waiting for LLDAP to be ready at $LLDAP_URL..."
timeout=300 # 5 minutes timeout
elapsed=0
until curl --silent --fail --connect-timeout 5 --max-time 10 "$LLDAP_URL/health" >/dev/null 2>&1; do
    if [ $elapsed -ge $timeout ]; then
        echo "Error: LLDAP did not become ready within $timeout seconds"
        exit 1
    fi
    echo "LLDAP not ready, waiting 5 seconds... (elapsed: ${elapsed}s)"
    sleep 5
    elapsed=$((elapsed + 5))
done
echo "LLDAP is ready!"

# Debug: Print environment variables (excluding sensitive ones)
echo "=== Environment Variables ==="
echo "LLDAP_URL: $LLDAP_URL"
echo "LLDAP_ADMIN_USERNAME: $LLDAP_ADMIN_USERNAME"
echo "LLDAP_LDAP_BASE_DN: $LLDAP_LDAP_BASE_DN"
echo "LLDAP_LDAP_USER_EMAIL: $LLDAP_LDAP_USER_EMAIL"
echo "USER_CONFIGS_DIR: $USER_CONFIGS_DIR"
echo "GROUP_CONFIGS_DIR: $GROUP_CONFIGS_DIR"
echo "DO_CLEANUP: $DO_CLEANUP"
echo "LLDAP_SET_PASSWORD_PATH: $LLDAP_SET_PASSWORD_PATH"
echo "============================="

# Run the bootstrap script
echo "Running LLDAP bootstrap.sh..."
if /app/bootstrap.sh; then
    echo "User sync completed successfully"
else
    echo "User sync failed"
    exit 1
fi
