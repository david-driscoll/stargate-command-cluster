#!/bin/sh
set -e
apk add garage
garage key import -n cluster-user \"$GARAGE_USER_CLUSTER_USER\" \"$GARAGE_PASSWORD_CLUSTER_USER\"
garage key allow --create-bucket cluster-user
garage bucket allow --read --write --owner cluster-user --key cluster-user
garage key import -n authentik \"$GARAGE_USER_AUTHENTIK\" \"$GARAGE_PASSWORD_AUTHENTIK\"
garage bucket create authentik/postgres
garage bucket allow --read --write --owner authentik/postgres --key authentik
garage bucket create iris
garage bucket allow --read --write --owner iris --key authentik
garage bucket website --allow iris
garage key import -n tivi-sync \"$GARAGE_USER_TIVI_SYNC\" \"$GARAGE_PASSWORD_TIVI_SYNC\"
garage bucket create tivi-cache
garage bucket allow --read --write --owner tivi-cache --key tivi-sync
garage bucket create tivi-results
garage bucket allow --read --write --owner tivi-results --key tivi-sync
garage bucket website --allow tivi-results