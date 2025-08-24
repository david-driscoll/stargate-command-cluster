#!/bin/sh

rclone mkdir /data/iris
rclone mkdir /data/backrest
rclone mkdir /data/tivi-cache
rclone mkdir /data/tivi-results
rclone serve s3 --cache-dir /cache --vfs-cache-mode writes --addr :8080 --auth-key $R3_USER_CLUSTER_USER,$R3_PASSWORD_CLUSTER_USER --auth-key $R3_USER_AUTHENTIK,$R3_PASSWORD_AUTHENTIK --auth-key $R3_USER_BACKREST,$R3_PASSWORD_BACKREST --auth-key $R3_USER_TIVI_SYNC,$R3_PASSWORD_TIVI_SYNC /data
