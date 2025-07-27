#!/bin/sh

set -e
CONFIG_PATH=/app/config/config.json

cat $CONFIG_PATH | jq
rm $CONFIG_PATH
# cat $CONFIG_PATH | jq ".instance = \"${CLUSTER_CNAME}\"" | jq ".version = 4" | jq ".auth.disabled = true" | jq ".plans = []" | jq ".repos = []" | tee $CONFIG_PATH
# cat $CONFIG_PATH | jq

# repo_json=$(jq -n --arg id "pgadmin" --arg uri "/shares/volsync/pgadmin" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "uptime-kuma" --arg uri "/shares/volsync/uptime-kuma" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "autokuma" --arg uri "/shares/volsync/autokuma" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "authentik" --arg uri "/shares/volsync/authentik" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "matter" --arg uri "/shares/volsync/matter" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "home-assistant" --arg uri "/shares/volsync/home-assistant" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "network-ups-tools" --arg uri "/shares/volsync/network-ups-tools" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "scrypted" --arg uri "/shares/volsync/scrypted" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "plex" --arg uri "/shares/volsync/plex" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "tautulli" --arg uri "/shares/volsync/tautulli" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "backrest" --arg uri "/shares/volsync/backrest" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# repo_json=$(jq -n --arg id "restic-server" --arg uri "/shares/volsync/restic-server" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, autoInitialize: true}');
# cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# cat $CONFIG_PATH | jq '.repos |= (group_by(.id) | map(.[0]))' | tee $CONFIG_PATH

