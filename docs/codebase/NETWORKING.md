# Networking

**Evidence:** `kubernetes/apps/network/`, `kubernetes/components/tailscale/`, `talos/talconfig.yaml`, `kubernetes/apps/tailscale-system/`

## Network Stack Overview

```
External Internet
      │
      ▼
Cloudflare (DNS + Tunnel)          Tailscale VPN
      │                                   │
      ▼                                   ▼
cloudflared (network ns)        Tailscale Operator
      │                          (tailscale-system ns)
      ▼                                   │
nginx-ingress (external class)    ingress class: tailscale
      │                                   │
      └──────────────┬────────────────────┘
                     ▼
              Cilium (eBPF CNI)
              Pod Network: 10.209.0.0/16
              Svc Network: 10.199.0.0/16
                     │
         ┌───────────┼───────────┐
         ▼           ▼           ▼
      Node 1       Node 2      Node 3
  10.10.209.10  10.10.209.11  10.10.209.12
```

## IP Addressing

```
Management Network: 10.10.0.0/16
  Router:           10.10.0.1
  milky-way:        10.10.209.10
  pegasus:          10.10.209.11
  othalla:          10.10.209.12
  API VIP:          10.10.209.201

Pod Network (Cilium): 10.209.0.0/16
Service Network:      10.199.0.0/16
```

## Cilium — Container Networking Interface

**Chart:** cilium/cilium v1.19.4 (bootstrap phase — `bootstrap/helmfile.yaml`)

Cilium provides eBPF-based networking with native load balancing, network policies, and observability. Installed before Flux during bootstrap because it is the cluster CNI.

- **Pod CIDR:** 10.209.0.0/16
- **Service CIDR:** 10.199.0.0/16
- **Mode:** eBPF native (no kube-proxy)
- **Network Policies:** Enabled — default deny ingress per namespace via `kubernetes/components/common/`
- **Metrics:** Exported to Prometheus in `observability` namespace

### Network Policy Pattern

```yaml
# Default deny ingress (applied via kubernetes/components/common/)
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
spec:
  podSelector: {}
  policyTypes: [Ingress]
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              name: network        # allow ingress controllers
```

## DNS Architecture — Split-Horizon

Internal and external DNS resolve the same hostnames to different destinations:

```
Internal (home network device):
  Query: home-assistant.sgc.driscoll.tech
  → Home DNS server forwards *.sgc.driscoll.tech to k8s_gateway (10.10.209.x)
  → k8s_gateway returns LoadBalancer ClusterIP
  → Traffic stays on home network

External (internet):
  Query: home-assistant.sgc.driscoll.tech
  → Cloudflare nameservers return tunnel endpoint
  → Traffic flows through Cloudflare Tunnel
```

### k8s_gateway (Internal DNS)

- **Namespace:** network
- **Purpose:** Resolves `*.sgc.driscoll.tech` to ingress service IPs for home network devices
- **Requirement:** Home DNS server must forward `sgc.driscoll.tech` to `10.10.209.x:53`

```bash
# Test from home network
dig @10.10.209.10 home-assistant.sgc.driscoll.tech
```

### external-dns (Cloudflare DNS)

- **Namespace:** network
- **Chart:** bitnami/external-dns
- **Provider:** Cloudflare
- **Behavior:** Watches Ingress/HTTPRoute resources; creates/updates Cloudflare A records automatically
- **Annotation required:** `external-dns.alpha.kubernetes.io/target: "tunnel.sgc.driscoll.tech"`

## Ingress Controllers

### nginx-ingress (Primary)

**Namespace:** network | **Chart:** ingress-nginx

Three IngressClasses, each serving a different access path:

| IngressClass | Access path | Who can reach it |
|--------------|------------|-----------------|
| `internal` | k8s_gateway → home network | Home network devices only |
| `external` | Cloudflare Tunnel → internet | Public internet |
| `tailscale` | Tailscale VPN | Tailnet devices (VPN) |

```yaml
# Internal ingress example
spec:
  ingressClassName: internal
  tls:
    - hosts: [home-assistant.sgc.driscoll.tech]
  rules:
    - host: home-assistant.sgc.driscoll.tech
```

### Traefik (Secondary/Alternative)

- **Namespace:** network — **Chart:** traefik/traefik
- Used for advanced routing with Middleware CRDs (rate limiting, auth headers, CORS)
- Manages its own IngressRoute CRDs (not standard Ingress)

## Cloudflare Integration

### Cloudflare Tunnel

Zero-trust external access — no ports exposed on the home network:

```
Internet User
      │ HTTPS
      ▼
Cloudflare Edge (worldwide PoP)
      │ (persistent outbound connection)
      ▼
cloudflared agent (network namespace)
      │
      ▼
nginx-ingress (external class)
      │
      ▼
Application pod
```

- **Credential:** `cloudflare-tunnel.json` (gitignored; stored in 1Password)
- **DNS:** `*.sgc.driscoll.tech` CNAME → `tunnel.sgc.driscoll.tech` (managed by external-dns)
- **TLS:** cert-manager issues Let's Encrypt certs via Cloudflare DNS01 challenge

## Tailscale VPN

The cluster uses the [Tailscale Kubernetes Operator](https://tailscale.com/kb/1236/kubernetes-operator) for VPN-based internal access. Two proxy groups serve different traffic directions.

### Proxy Groups

| Proxy Group | Direction | Purpose |
|-------------|-----------|---------|
| `tailnet-outbound` | Cluster → tailnet devices | Expose cluster services to VPN users |
| `tailnet-inbound` | External tailnet host → cluster | Proxy external services into the cluster |

### Outbound — Cluster Services to VPN Devices

Apps use `ingressClassName: tailscale` with the `tailnet-outbound` proxy group. All apps are tagged `tag:apps` for ACL control.

```yaml
# kubernetes/components/tailscale/ingress.yaml (reusable component)
metadata:
  annotations:
    tailscale.com/proxy-group: tailnet-outbound
    tailscale.com/experimental-forward-cluster-traffic-via-ingress: "true"
    tailscale.com/tags: "tag:apps"
spec:
  ingressClassName: tailscale
  defaultBackend:
    service:
      name: "${TAILSCALE_SERVICE:=${APP}}"
      port:
        name: "${TAILSCALE_PORT:=http}"
  tls:
    - hosts: ["${TAILSCALE_HOST:=${APP}}"]
```

To expose an app on Tailscale, add the component to its kustomization:

```yaml
components:
  - ../../../../components/tailscale
```

### Inbound — External Tailnet Services into Cluster

Services running on other tailnet nodes (outside the cluster) can be proxied into the cluster using `ExternalName` services with `tailscale.com/tailnet-fqdn`. This makes an external tailnet host addressable from inside the cluster.

```yaml
# Example: alertmanager on the tailnet → accessible inside cluster
metadata:
  annotations:
    tailscale.com/tailnet-fqdn: "alertmanager.${TAILSCALE_DOMAIN}"
    tailscale.com/proxy-group: tailnet-inbound
spec:
  type: ExternalName
  externalName: unused
  ports:
    - port: 9093
      name: http
```

**File:** `kubernetes/apps/observability/prometheus-proxy/alertmanager-operated.yaml`

### Tailscale Identity Provider (tsidp)

A Tailscale OIDC identity provider runs at hostname `idp` on the tailnet:

- **Image:** `ghcr.io/tailscale/tsidp:v0.0.12`
- **Namespace:** tailscale-system
- **Hostname on tailnet:** `idp`
- **Purpose:** Issues OIDC tokens backed by Tailscale device identities for SSO
- **STS:** Enabled (`TSIDP_ENABLE_STS=1`) for service-to-service auth
- **State:** Persisted to a PVC (`/data`)
- **File:** `kubernetes/apps/tailscale-system/idp/tsidp.yaml`

### Tailscale-Only Services

These services are only reachable when connected to the Tailscale tailnet:

| Service | Tailscale hostname | Port | Purpose |
|---------|--------------------|------|---------|
| Alertmanager | `alertmanager.driscoll.tech` | 443 | Cluster alert API and UI |
| Tailscale IDP | `idp` (tailnet only) | 443 | OIDC identity provider |
| glance-k8s | `glance-k8s.sgc.internal` | 443 | K8s dashboard widgets |

```bash
# Verify Tailscale connectivity
tailscale status
tailscale ping alertmanager

# Check tailscale operator in cluster
kubectl -n tailscale-system get pods
kubectl -n tailscale-system get proxygroups

# Check tsidp logs
kubectl -n tailscale-system logs -l app.kubernetes.io/name=tsidp -f
```

## Alertmanager — Cluster Observability Endpoint

Alertmanager is the primary troubleshooting starting point for cluster-wide issues. It is exposed on the Tailscale network via `tailnet-inbound`.

- **External URL (Tailscale required):** `https://alertmanager.driscoll.tech`
- **Alerts API:** `https://alertmanager.driscoll.tech/api/v2/alerts`
- **Internal cluster URL:** `http://alertmanager-operated.observability.svc.cluster.local:9093/api/v2/alerts/`
- **Port:** 9093

```bash
# Query all active alerts (requires Tailscale connection)
curl https://alertmanager.driscoll.tech/api/v2/alerts | jq '.[].labels'

# Filter to critical alerts only
curl 'https://alertmanager.driscoll.tech/api/v2/alerts?filter=severity%3D%22critical%22' | jq .

# View alert groups
curl https://alertmanager.driscoll.tech/api/v2/alerts/groups | jq .

# Query from inside the cluster (no Tailscale needed)
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://alertmanager-operated.observability.svc.cluster.local:9093/api/v2/alerts | jq .
```

**Flux Alert Integration:** Flux sends GitOps reconciliation events to alertmanager via:

```yaml
# kubernetes/components/alerts/alertmanager/provider.yaml
apiVersion: notification.toolkit.fluxcd.io/v1beta3
kind: Provider
spec:
  type: alertmanager
  address: http://alertmanager-operated.observability.svc.cluster.local:9093/api/v2/alerts/
```

## Certificates & TLS

- **Provider:** cert-manager v1.20.2 (`cert-manager` namespace)
- **Issuer:** Let's Encrypt production (ACME DNS01 via Cloudflare)
- **Annotation:** `cert-manager.io/cluster-issuer: letsencrypt-prod`
- **Renewal:** Automatic at 30 days before expiry
- **Wildcard:** `*.sgc.driscoll.tech` supported via DNS01 challenge

## Prometheus Metrics

```promql
# Request rate by ingress class
rate(nginx_requests_total[5m]) by (ingress_class)

# Cilium policy violations
increase(cilium_policy_denied[5m])

# Longhorn volume usage
longhorn_volume_usage_bytes
```

## Troubleshooting

```bash
# DNS not resolving (internal)
dig @10.10.209.10 home-assistant.sgc.driscoll.tech
kubectl -n kube-system logs -l k8s-app=kube-dns

# Ingress not working
kubectl -n sgc get ingress -o wide
kubectl -n sgc get endpoints home-assistant
kubectl -n network logs -l app.kubernetes.io/name=ingress-nginx

# External DNS not syncing
kubectl -n network logs -l app=external-dns

# Cloudflare tunnel issues
kubectl -n network logs -l app=cloudflared

# Tailscale VPN issues
kubectl -n tailscale-system get pods
kubectl -n tailscale-system logs -l app=tailscale-operator

# Active alerts (Tailscale required)
curl https://alertmanager.driscoll.tech/api/v2/alerts | jq '.[].labels'
```
