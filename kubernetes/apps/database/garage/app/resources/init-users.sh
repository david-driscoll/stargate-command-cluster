#!/bin/sh
set -e
curl -L -o /tmp/garage https://garagehq.deuxfleurs.fr/_releases/v2.0.0/x86_64-unknown-linux-musl/garage && chmod +x /tmp/garage
cat /mnt/garage.toml
cat /data/garage.toml
/tmp/garage key import -n cluster-user \"$GARAGE_USER_CLUSTER_USER\" \"$GARAGE_PASSWORD_CLUSTER_USER\"
/tmp/garage key allow --create-bucket cluster-user
/tmp/garage bucket allow --read --write --owner cluster-user --key cluster-user
/tmp/garage key import -n authentik \"$GARAGE_USER_AUTHENTIK\" \"$GARAGE_PASSWORD_AUTHENTIK\"
/tmp/garage bucket create authentik/postgres
/tmp/garage bucket allow --read --write --owner authentik/postgres --key authentik
/tmp/garage bucket create iris
/tmp/garage bucket allow --read --write --owner iris --key authentik
/tmp/garage bucket website --allow iris
/tmp/garage key import -n postgres-sgc \"$GARAGE_USER_POSTGRES_SGC\" \"$GARAGE_PASSWORD_POSTGRES_SGC\"
/tmp/garage bucket create postgres
/tmp/garage bucket allow --read --write --owner postgres --key postgres-sgc
/tmp/garage key import -n tivi-sync \"$GARAGE_USER_TIVI_SYNC\" \"$GARAGE_PASSWORD_TIVI_SYNC\"
/tmp/garage bucket create tivi-cache
/tmp/garage bucket allow --read --write --owner tivi-cache --key tivi-sync
/tmp/garage bucket create tivi-results
/tmp/garage bucket allow --read --write --owner tivi-results --key tivi-sync
/tmp/garage bucket website --allow tivi-results