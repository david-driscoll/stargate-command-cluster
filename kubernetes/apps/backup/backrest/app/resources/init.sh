#!/bin/sh

set -e
CONFIG_PATH=/app/config/config.json

if [ ! -f "$CONFIG_PATH" ]; then
  echo "{\"repos\": []}" > $CONFIG_PATH
fi
cat $CONFIG_PATH | jq ".instance = \"${CLUSTER_CNAME}\"" | jq ".version = 4" | jq ".auth.disabled = true" | jq ".plans = []" | jq ".repos = []" | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "pgadmin" --arg uri "/shares/volsync/pgadmin" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "uptime-kuma" --arg uri "/shares/volsync/uptime-kuma" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "autokuma" --arg uri "/shares/volsync/autokuma" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "authentik" --arg uri "/shares/volsync/authentik" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "matter" --arg uri "/shares/volsync/matter" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "home-assistant" --arg uri "/shares/volsync/home-assistant" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "network-ups-tools" --arg uri "/shares/volsync/network-ups-tools" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "scrypted" --arg uri "/shares/volsync/scrypted" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "plex" --arg uri "/shares/volsync/plex" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "tautulli" --arg uri "/shares/volsync/tautulli" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "backrest" --arg uri "/shares/volsync/backrest" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
repo_json=$(jq -n --arg id "restic-server" --arg uri "/shares/volsync/restic-server" --arg password "RESTIC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
cat $CONFIG_PATH | jq '.repos |= (group_by(.id) | map(.[0]))' | tee $CONFIG_PATH

cat $CONFIG_PATH | jq
