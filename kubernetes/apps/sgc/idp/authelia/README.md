# Authelia Configuration

This directory contains the Authelia identity provider configuration for the Stargate Command cluster.

## Overview

Authelia is configured to:
- Use LDAP (lldap) for user authentication and management
- Provide OIDC authentication for applications
- Support multiple OIDC clients (e.g., Tailscale, Plex, and other applications)
- Store session data in Redis (Valkey)
- Store configuration data in PostgreSQL

## Secret Management

All sensitive configuration is stored in OnePassword and synchronized using External Secrets Operator. The secrets are automatically updated when changed in OnePassword without requiring manifest updates.

### Required OnePassword Secrets

#### 1. `Authelia Config`
Contains the main Authelia configuration secrets:
- `jwt_secret`: Secret for JWT token signing
- `session_secret`: Secret for session encryption
- `storage_encryption_key`: Key for encrypting data in storage
- `oidc_hmac_secret`: HMAC secret for OIDC
- `oidc_jwks_key`: RSA private key for OIDC (PEM format)
- `root_domain`: Base domain for the cluster (e.g., example.com)
- `smtp_host`: SMTP server hostname
- `smtp_port`: SMTP server port (typically 465 or 587)
- `smtp_username`: SMTP authentication username
- `smtp_password`: SMTP authentication password
- `smtp_from`: Email address to send from
- `smtp_startup_check`: Email address for startup health check
- `oidc_clients`: JSON array of OIDC client configurations (see below)

#### 2. `LLDAP Admin`
Contains LDAP admin credentials:
- `password`: LLDAP admin password

#### 3. `authelia-postgres` (ClusterSecretStore: database)
Database credentials automatically managed by CloudNative-PG:
- `hostname`: PostgreSQL hostname
- `port`: PostgreSQL port
- `database`: Database name
- `username`: Database username
- `password`: Database password

### OIDC Clients Configuration

The `oidc_clients` field in the `Authelia Config` secret should contain a JSON array with client configurations. This allows you to add, remove, or modify clients without updating the Kubernetes manifests.

Example JSON structure:
```json
[
  {
    "client_id": "tailscale",
    "client_name": "Tailscale",
    "client_secret": "$argon2id$v=19$m=65536,t=3,p=4$...",
    "public": false,
    "authorization_policy": "one_factor",
    "redirect_uris": [
      "https://login.tailscale.com/oidc/callback"
    ],
    "scopes": ["openid", "profile", "email", "groups"],
    "grant_types": ["authorization_code"],
    "response_types": ["code"],
    "token_endpoint_auth_method": "client_secret_basic"
  },
  {
    "client_id": "plex",
    "client_name": "Plex Media Server",
    "client_secret": "$argon2id$v=19$m=65536,t=3,p=4$...",
    "public": false,
    "authorization_policy": "one_factor",
    "redirect_uris": [
      "https://plex.example.com/auth/callback",
      "https://app.plex.tv/auth/callback"
    ],
    "scopes": ["openid", "profile", "email"],
    "grant_types": ["authorization_code"],
    "response_types": ["code"],
    "token_endpoint_auth_method": "client_secret_post"
  }
]
```

### Generating Client Secrets

Client secrets should be hashed using Argon2id. You can generate them using the Authelia CLI:

```bash
docker run --rm authelia/authelia:latest authelia crypto hash generate argon2 --password 'your-secret-here'
```

## Architecture

```
┌─────────────────┐
│   Applications  │
│  (Tailscale,    │
│   Plex, etc.)   │
└────────┬────────┘
         │ OIDC
         ▼
┌─────────────────┐
│    Authelia     │
│  (OIDC Provider)│
└────────┬────────┘
         │ LDAP
         ▼
┌─────────────────┐
│      LLDAP      │
│ (User Directory)│
└─────────────────┘
```

## Access Control

Default access control rules:
- Authelia UI (`authelia.*` and `auth.*`): Bypass authentication (public access to login page)
- All other domains (`*.*`): Require one-factor authentication for users in `admins` or `users` groups

## Monitoring

Authelia exposes Prometheus metrics on port 9959 at `/metrics`. A ServiceMonitor is configured for automatic scraping by Prometheus.

## Ingress

Authelia is accessible via:
- `https://authelia.${ROOT_DOMAIN}` (primary)
- `https://auth.${ROOT_DOMAIN}` (alias)

Traffic is routed through the internal Gateway using HTTPRoute and exposed via Tailscale.

## Adding New OIDC Clients

To add a new OIDC client (e.g., for integrating a new application):

1. Update the `oidc_clients` JSON array in the `Authelia Config` OnePassword secret
2. Add the new client configuration with:
   - `client_id`: Unique identifier
   - `client_name`: Human-readable name
   - `client_secret`: Argon2id-hashed secret
   - `redirect_uris`: List of allowed callback URLs
   - Other required OIDC parameters
3. Save the secret in OnePassword
4. Wait for External Secrets to sync (approximately 4 minutes)
5. Authelia will automatically reload with the new configuration

No Kubernetes manifest changes or pod restarts are required!
