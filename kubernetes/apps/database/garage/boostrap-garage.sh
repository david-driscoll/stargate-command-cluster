kubectl exec --stdin --tty -n database garage-0 -- ./garage status
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 3a6b5eac0172d58a4c03514d1e7a546091c60ddc7ea2e8bf655b5760b4be0222 -z sgc -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 7331c42992d16d73ef5d125b27b04efc5321698e636c19e27f6db68f896629c5 -z sgc -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 3d7bd531e2853b0f29d8291084c2ecddc5b4eb642c3b2957786983a823603e17 -z sgc -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout show;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout apply --version 1;
