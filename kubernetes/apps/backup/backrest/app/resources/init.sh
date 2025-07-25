#!/bin/sh

set -e

jq '.instance = ${CLUSTER_CNAME}' /data/config.json > /data/config.tmp && mv /data/config.tmp /data/config.json
jq '.auth.disabled = true' /data/config.json > /data/config.tmp && mv /data/config.tmp /data/config.json
repo_json=$(jq -n --arg id "pgadmin" --arg uri "/shares/volsync/pgadmin" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "uptime-kuma" --arg uri "/shares/volsync/uptime-kuma" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "autokuma" --arg uri "/shares/volsync/autokuma" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "authentik" --arg uri "/shares/volsync/authentik" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "matter" --arg uri "/shares/volsync/matter" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "home-assistant" --arg uri "/shares/volsync/home-assistant" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "network-ups-tools" --arg uri "/shares/volsync/network-ups-tools" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "scrypted" --arg uri "/shares/volsync/scrypted" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "plex" --arg uri "/shares/volsync/plex" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "tautulli" --arg uri "/shares/volsync/tautulli" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "backrest" --arg uri "/shares/volsync/backrest" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
repo_json=$(jq -n --arg id "restic-server" --arg uri "/shares/volsync/restic-server" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
jq --argjson repo "$repo_json" '.repos += [$repo]' /app/config.json > /app/config.tmp && mv /app/config.tmp /app/config.json
