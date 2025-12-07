---
applyTo: '**'
---

# Flux

Flux is a tool for managing Kubernetes clusters using GitOps principles. The Flux Kubernetes Operator is responsible for reconciling the desired state defined in Git repositories with the actual state of the cluster.

When working with Flux and the Kubernetes Operator, consider the following best practices:

Flux has the concept of "sources" and "reconciliations." Sources define where the desired state is stored (e.g., Git repositories, Helm repositories), while reconciliations define how and when to apply that state to the cluster.

# Kubernetes

Kubernetes is an open-source container orchestration platform that automates the deployment, scaling, and management of containerized applications. When using Flux with Kubernetes, ensure that your cluster is properly configured to support GitOps workflows.

When defining Kubernetes resources, use declarative YAML manifests to specify the desired state of your applications and infrastructure. This allows Flux to monitor and apply changes automatically.

# App Template
The App Template is a Helm chart that provides a standardized way to deploy applications in a Kubernetes cluster. It includes common configurations and best practices for deploying applications using Helm.

When using the App Template with Flux, ensure that you define your Helm releases in Flux's HelmRelease resources. This allows Flux to manage the lifecycle of your applications based on the desired state defined in your Git repositories.

# Best Practices

The kubeconfig file is kubeconfig
The talosconfig fils is talos/clusterconfig/talosconfig

* Use the flux mcp when possible.
* Use the kubernetes mcp when possible.
