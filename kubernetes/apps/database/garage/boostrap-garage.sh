kubectl exec --stdin --tty -n database garage-0 -- ./garage status
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign ff44f5f7bfd3374d1c76a01acd987a3b8b1e04678fdfaceb6605533339b0ea71 -z sgc -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 9ab259b31473b358 -z one -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign 24bfa6d6eb202ed1 -z two -c 64G;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout show;
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout apply --version 1;
