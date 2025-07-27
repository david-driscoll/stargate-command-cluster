#!/bin/sh

set -e
CONFIG_PATH=/app/config/config.json

if [ ! -f "$CONFIG_PATH" ]; then
  echo "{\"repos\": []}" > $CONFIG_PATH
fi
cat $CONFIG_PATH | jq ".instance = \"${CLUSTER_CNAME}\"" | jq ".version = 4" | jq ".auth.disabled = true" | jq ".plans = []" | jq ".repos = []" | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
# Prepare repo_json for volsync volume
repo_json=$(jq -n \
  --arg id "${volume}" \  # Volume ID
  --arg uri "/shares/volsync/${volume}" \
  --arg password "RESTIC_PASSWORD" \
  '{
    id: $id,
    uri: $uri,
    password: $password,
    prunePolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      maxUnusedPercent: 10
    },
    checkPolicy: {
      schedule: { disabled: true, clock: "CLOCK_LAST_RUN_TIME" },
      readDataSubsetPercent: 0
    },
    commandPrefix: {}
  }'
)
cat $CONFIG_PATH | jq --argjson repo "$repo_json" '.repos += [$repo]' | tee $CONFIG_PATH
cat $CONFIG_PATH | jq '.repos |= (group_by(.id) | map(.[0]))' | tee $CONFIG_PATH

cat $CONFIG_PATH | jq
