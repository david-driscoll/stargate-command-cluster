#!/bin/sh
set -e

# Required ENV variables: MINIO_ENDPOINT, MINIO_ACCESS_KEY, MINIO_SECRET_KEY, MINIO_BUCKET
# Optional: MINIO_REGION, MINIO_USE_SSL

if ! command -v mc >/dev/null 2>&1; then
    echo "MinIO client (mc) not found. Installing..."
    curl -sSL https://dl.min.io/client/mc/release/linux-amd64/mc -o /usr/local/bin/mc
    chmod +x /usr/local/bin/mc
fi

MC_ALIAS=minio
MC_CONFIG_DIR="/tmp/.mc"
export MC_CONFIG_DIR

mc alias set "$MC_ALIAS" "$MINIO_ENDPOINT" "$MINIO_ACCESS_KEY" "$MINIO_SECRET_KEY"

# Copy all files from the bucket to /config
mc cp --recursive "$MC_ALIAS/$MINIO_BUCKET" /config

echo "Files copied from $MINIO_BUCKET to /config."
