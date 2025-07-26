#!/bin/sh

set -e
CONFIG_PATH=/app/config.json
TEMP_CONFIG_PATH=/tmp/config.json.tmp

echo "{\"repos\": []}" > $CONFIG_PATH
cat $CONFIG_PATH | jq
jq ".instance = \"${CLUSTER_CNAME}\"" $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
jq ".version = 4" $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
jq ".auth.disabled = true" $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "pgadmin" --arg uri "/shares/volsync/pgadmin" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "uptime-kuma" --arg uri "/shares/volsync/uptime-kuma" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "autokuma" --arg uri "/shares/volsync/autokuma" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "authentik" --arg uri "/shares/volsync/authentik" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "matter" --arg uri "/shares/volsync/matter" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "home-assistant" --arg uri "/shares/volsync/home-assistant" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "network-ups-tools" --arg uri "/shares/volsync/network-ups-tools" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "scrypted" --arg uri "/shares/volsync/scrypted" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "plex" --arg uri "/shares/volsync/plex" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "tautulli" --arg uri "/shares/volsync/tautulli" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "backrest" --arg uri "/shares/volsync/backrest" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
repo_json=$(jq -n --arg id "restic-server" --arg uri "/shares/volsync/restic-server" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password, auto_initialize: true}');
jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
# remove any duplicate repos by id
jq '.repos |= (group_by(.id) | map(.[0]))' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
cat /app/config.json | jq
