#!/bin/bash
DIR="/etc/ssl/private"
NAME="$(tailscale status --json | jq '.Self.DNSName | .[:-1]' -r)"
tailscale cert --cert-file="${DIR}/${NAME}.crt" --key-file="${DIR}/${NAME}.key" "${NAME}"

# for PVE
pvenode cert set "${DIR}/${NAME}.crt" "${DIR}/${NAME}.key" --force --restart
