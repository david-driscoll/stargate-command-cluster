kubectl exec --stdin --tty -n database garage-0 -- ./garage status
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 88d0992a0c66365f -z zero -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 9ab259b31473b358 -z one -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 24bfa6d6eb202ed1 -z two -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout show;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout apply --version 1;
