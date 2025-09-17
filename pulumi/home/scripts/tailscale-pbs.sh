#!/bin/bash
DIR="/etc/ssl/private"
NAME="$(tailscale status --json | jq '.Self.DNSName | .[:-1]' -r)"
tailscale cert --cert-file="${DIR}/${NAME}.crt" --key-file="${DIR}/${NAME}.key" "${NAME}"

# for PVE
pvenode cert set "${DIR}/${NAME}.crt" "${DIR}/${NAME}.key" --force --restart

# for PBS
cp ${DIR}/${NAME}.crt /etc/proxmox-backup/proxy.pem
cp ${DIR}/${NAME}.key /etc/proxmox-backup/proxy.key
chmod 640 /etc/proxmox-backup/proxy.key /etc/proxmox-backup/proxy.pem
chgrp backup /etc/proxmox-backup/proxy.key /etc/proxmox-backup/proxy.pem
systemctl reload proxmox-backup-proxy.service
