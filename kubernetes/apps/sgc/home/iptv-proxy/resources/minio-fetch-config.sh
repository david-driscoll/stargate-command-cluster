#!/bin/sh
set -e

apt-get update
apt-get install -y curl

curl https://dl.min.io/client/mc/release/linux-amd64/mc \
    --create-dirs \
    -o /tmp/mc

chmod +x /tmp/mc
export PATH=$PATH:/tmp

MC_ALIAS=minio
MC_CONFIG_DIR="/tmp/.mc"
export MC_CONFIG_DIR

mc alias set "$MC_ALIAS" "$MINIO_ENDPOINT" "$MINIO_ACCESS_KEY" "$MINIO_SECRET_KEY"

# Copy all files from the bucket to /config
mc cp --recursive "$MC_ALIAS/$MINIO_BUCKET" /config

echo "Files copied from $MINIO_BUCKET to /config."
