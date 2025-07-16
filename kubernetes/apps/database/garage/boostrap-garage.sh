kubectl exec --stdin --tty -n database garage-0 -- ./garage status
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout assign xyz -z zero -c 64G
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout show
kubectl exec --stdin --tty -n database garage-0 -- ./garage layout apply --version 1
