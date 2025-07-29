#!/bin/bash

go run tailscale.com/cmd/tsidp@latest -ephemeral -reusable -preauth -tags "tag:${CLUSTER_CNAME}"
