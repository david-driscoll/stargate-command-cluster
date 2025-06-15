#!/bin/sh
set -e

MC_ALIAS=minio
MC_CONFIG_DIR="/tmp/.mc"
export MC_CONFIG_DIR

mc alias set "$MC_ALIAS" "$MINIO_ENDPOINT" "$MINIO_ACCESS_KEY" "$MINIO_SECRET_KEY"

# Copy all files from the bucket to /cache
mc cp "$MC_ALIAS/$MINIO_BUCKET/$TIVI_HOSTNAME.m3u" /cache/$TIVI_HOSTNAME.m3u
mc cp "$MC_ALIAS/$MINIO_BUCKET/$TIVI_HOSTNAME.xml" /cache/$TIVI_HOSTNAME.xml

echo "Files copied from $MINIO_BUCKET to /cache/."
ls -al /cache/
