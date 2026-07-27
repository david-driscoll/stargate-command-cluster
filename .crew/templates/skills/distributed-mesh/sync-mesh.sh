#!/bin/bash
# sync-mesh.sh — Materialize remote crew state locally
#
# Reads mesh.json, fetches remote crews into local directories.
# Run before agent reads. No daemon. No service. ~40 lines.
#
# Usage: ./sync-mesh.sh [path-to-mesh.json]
#        ./sync-mesh.sh --init [path-to-mesh.json]
# Requires: jq (https://github.com/jqlang/jq), git, curl

set -euo pipefail

# Handle --init mode
if [ "${1:-}" = "--init" ]; then
  MESH_JSON="${2:-mesh.json}"
  
  if [ ! -f "$MESH_JSON" ]; then
    echo "❌ $MESH_JSON not found"
    exit 1
  fi
  
  echo "🚀 Initializing mesh state repository..."
  crews=$(jq -r '.crews | keys[]' "$MESH_JSON")
  
  # Create crew directories with placeholder SUMMARY.md
  for crew in $crews; do
    if [ ! -d "$crew" ]; then
      mkdir -p "$crew"
      echo "  ✓ Created $crew/"
    else
      echo "  • $crew/ exists (skipped)"
    fi
    
    if [ ! -f "$crew/SUMMARY.md" ]; then
      echo -e "# $crew\n\n_No state published yet._" > "$crew/SUMMARY.md"
      echo "  ✓ Created $crew/SUMMARY.md"
    else
      echo "  • $crew/SUMMARY.md exists (skipped)"
    fi
  done
  
  # Generate root README.md
  if [ ! -f "README.md" ]; then
    {
      echo "# Crew Mesh State Repository"
      echo ""
      echo "This repository tracks published state from participating crews."
      echo ""
      echo "## Participating Crews"
      echo ""
      for crew in $crews; do
        zone=$(jq -r ".crews.\"$crew\".zone" "$MESH_JSON")
        echo "- **$crew** (Zone: $zone)"
      done
      echo ""
      echo "Each crew directory contains a \`SUMMARY.md\` with their latest published state."
      echo "State is synchronized using \`sync-mesh.sh\` or \`sync-mesh.ps1\`."
    } > README.md
    echo "  ✓ Created README.md"
  else
    echo "  • README.md exists (skipped)"
  fi
  
  echo ""
  echo "✅ Mesh state repository initialized"
  exit 0
fi

MESH_JSON="${1:-mesh.json}"

# Zone 2: Remote-trusted — git clone/pull
for crew in $(jq -r '.crews | to_entries[] | select(.value.zone == "remote-trusted") | .key' "$MESH_JSON"); do
  source=$(jq -r ".crews.\"$crew\".source" "$MESH_JSON")
  ref=$(jq -r ".crews.\"$crew\".ref // \"main\"" "$MESH_JSON")
  target=$(jq -r ".crews.\"$crew\".sync_to" "$MESH_JSON")

  if [ -d "$target/.git" ]; then
    git -C "$target" pull --rebase --quiet 2>/dev/null \
      || echo "⚠ $crew: pull failed (using stale)"
  else
    mkdir -p "$(dirname "$target")"
    git clone --quiet --depth 1 --branch "$ref" "$source" "$target" 2>/dev/null \
      || echo "⚠ $crew: clone failed (unavailable)"
  fi
done

# Zone 3: Remote-opaque — fetch published contracts
for crew in $(jq -r '.crews | to_entries[] | select(.value.zone == "remote-opaque") | .key' "$MESH_JSON"); do
  source=$(jq -r ".crews.\"$crew\".source" "$MESH_JSON")
  target=$(jq -r ".crews.\"$crew\".sync_to" "$MESH_JSON")
  auth=$(jq -r ".crews.\"$crew\".auth // \"\"" "$MESH_JSON")

  mkdir -p "$target"
  auth_flag=""
  if [ "$auth" = "bearer" ]; then
    token_var="$(echo "${crew}" | tr '[:lower:]-' '[:upper:]_')_TOKEN"
    [ -n "${!token_var:-}" ] && auth_flag="--header \"Authorization: Bearer ${!token_var}\""
  fi

  eval curl --silent --fail $auth_flag "$source" -o "$target/SUMMARY.md" 2>/dev/null \
    || echo "# ${crew} — unavailable ($(date))" > "$target/SUMMARY.md"
done

echo "✓ Mesh sync complete"
