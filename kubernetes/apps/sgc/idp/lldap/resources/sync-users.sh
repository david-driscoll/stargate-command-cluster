#!/bin/bash
set -euo pipefail

echo "Starting LLDAP user sync using bootstrap.sh..."

# Check if bootstrap.sh exists
if [ ! -f "/app/bootstrap.sh" ]; then
    echo "Error: bootstrap.sh not found at /app/bootstrap.sh"
    echo "This script requires the LLDAP bootstrap.sh script to be available"
    exit 1
fi

# Run the bootstrap script
echo "Running LLDAP bootstrap.sh..."
if /app/bootstrap.sh; then
    echo "User sync completed successfully"
else
    echo "User sync failed"
    exit 1
fi
