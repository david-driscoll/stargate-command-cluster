# GitHub Copilot Guide for Stargate Command Cluster

## Repository Overview

This repository contains the configuration for a Kubernetes cluster managed with Flux CD and follows GitOps principles. The repository is organized as follows:

-   `/kubernetes`: Contains all Kubernetes manifests

    -   `/apps`: Application deployments organized by namespace
    -   `/bootstrap`: Cluster bootstrap configurations
    -   `/flux`: Flux system configurations
    -   `/components`: Reusable components for applications

-   `/talos`: Contains Talos configurations for the cluster nodes

-   `/scripts`: Contains helper scripts for cluster management

## Common Operations

### Adding a New Application

When adding a new application to the cluster:

1. Create a new directory under `/kubernetes/apps/[NAMESPACE]/[APP_NAME]`
2. Follow the existing pattern of creating:
    - `helmrelease.yaml` - Main application deployment
    - `config.yaml` - Application configuration
    - `kustomization.yaml` - Resource list
    - `ks.yaml` - Flux Kustomization object

Some applications may be be found using the website https://kubesearch.dev/


### Authentication Patterns

Applications that require authentication should use one of:

1. **Authelia** - For web applications requiring SSO

    - Use the Traefik middleware `authelia-auth` in the `sgc` namespace
    - See `/kubernetes/apps/sgc/idp/authelia` for reference

2. **Tailscale** - For secure access to services
    - See `/kubernetes/apps/sgc/idp/tailscale` for reference

### Secrets Management

Secrets are encrypted using SOPS with Age.

Also secrets can come from 1Password k8s operator, or from the external secrets operator.

Generally new secrets should always be stored in the 1password vault.

### Volume Management

Persistent volumes are managed using:

1. **VolSync** for backups
2. Direct PVC claims for storage

## Helper Commands

Common `task` commands:

```bash
# Get cluster status
task cluster:status

# Update Flux resources
task cluster:apply

# Reconcile Flux resources
task flux:reconcile

# Update Helm releases
task helm:update
```

## Special Patterns

When writing configurations for this cluster, remember:

1. Use Helm releases with the `app-template` pattern for most applications
2. Use ConfigMap for application configurations
3. Use SOPS for secret management
4. Follow the established directory structure

## Repository Best Practices

1. Never commit unencrypted secrets
2. Always test changes in a development environment first
3. Use labels consistently for resources
4. Follow naming conventions for resources
