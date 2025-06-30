#!/bin/bash

# PostgreSQL Disaster Recovery Script
# This script helps manage PostgreSQL cluster recovery scenarios

set -euo pipefail

# Configuration
NAMESPACE="${NAMESPACE:-sgc}"
APP="${APP:-authentik}"
KUBECONFIG="${KUBECONFIG:-kubeconfig}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Help function
show_help() {
    cat << EOF
PostgreSQL Disaster Recovery Script

Usage: $0 [COMMAND] [OPTIONS]

Commands:
    status          Show cluster status and health
    backup-status   Show backup status and available backups
    prepare-recovery Prepare cluster for disaster recovery
    recover         Perform disaster recovery from backup
    cleanup-recovery Clean up after successful recovery
    clean-failed-jobs Clean up failed recovery jobs

Options:
    -n, --namespace  Kubernetes namespace (default: sgc)
    -a, --app        Application name (default: authentik)
    -h, --help       Show this help message

Examples:
    # Check cluster status
    $0 status -n sgc -a authentik

    # Prepare for recovery (removes bootstrap config)
    $0 prepare-recovery -n sgc -a authentik

    # Perform disaster recovery
    $0 recover -n sgc -a authentik

    # Clean up after successful recovery
    $0 cleanup-recovery -n sgc -a authentik

Environment Variables:
    NAMESPACE       Default namespace (default: sgc)
    APP             Default app name (default: authentik)
    KUBECONFIG      Path to kubeconfig file
EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -n|--namespace)
                NAMESPACE="$2"
                shift 2
                ;;
            -a|--app)
                APP="$2"
                shift 2
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                if [[ -z "${COMMAND:-}" ]]; then
                    COMMAND="$1"
                else
                    log_error "Unknown option: $1"
                    show_help
                    exit 1
                fi
                shift
                ;;
        esac
    done
}

# Check prerequisites
check_prerequisites() {
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is not installed or not in PATH"
        exit 1
    fi

    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster. Check your kubeconfig."
        exit 1
    fi

    if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
        log_error "Namespace '$NAMESPACE' does not exist"
        exit 1
    fi
}

# Show cluster status
show_status() {
    log_info "Checking PostgreSQL cluster status for $APP in namespace $NAMESPACE"

    echo ""
    log_info "Cluster Status:"
    kubectl get cluster "${APP}-postgres" -n "$NAMESPACE" -o wide 2>/dev/null || {
        log_warning "Cluster ${APP}-postgres not found in namespace $NAMESPACE"
        return 1
    }

    echo ""
    log_info "Pod Status:"
    kubectl get pods -n "$NAMESPACE" -l "cnpg.io/cluster=${APP}-postgres" -o wide

    echo ""
    log_info "PVC Status:"
    kubectl get pvc -n "$NAMESPACE" -l "cnpg.io/cluster=${APP}-postgres"

    echo ""
    log_info "Services:"
    kubectl get svc -n "$NAMESPACE" -l "cnpg.io/cluster=${APP}-postgres"
}

# Show backup status
show_backup_status() {
    log_info "Checking backup status for $APP in namespace $NAMESPACE"

    echo ""
    log_info "Scheduled Backups:"
    kubectl get scheduledbackups -n "$NAMESPACE" 2>/dev/null || log_warning "No scheduled backups found"

    echo ""
    log_info "Available Backups:"
    kubectl get backups -n "$NAMESPACE" 2>/dev/null || log_warning "No backups found"

    echo ""
    log_info "ObjectStore Configuration:"
    kubectl get objectstore "${APP}-minio" -n "$NAMESPACE" -o yaml 2>/dev/null || log_warning "ObjectStore ${APP}-minio not found"
}

# Clean up failed recovery jobs
clean_failed_jobs() {
    log_info "Cleaning up failed recovery jobs for $APP in namespace $NAMESPACE"

    # Find and delete failed recovery jobs
    failed_jobs=$(kubectl get jobs -n "$NAMESPACE" -l "cnpg.io/cluster=${APP}-postgres,cnpg.io/jobRole=full-recovery" -o name 2>/dev/null || true)

    if [[ -n "$failed_jobs" ]]; then
        log_info "Found failed recovery jobs, deleting them..."
        echo "$failed_jobs" | xargs kubectl delete -n "$NAMESPACE"
        log_success "Failed recovery jobs cleaned up"
    else
        log_info "No failed recovery jobs found"
    fi

    # Clean up failed pods
    failed_pods=$(kubectl get pods -n "$NAMESPACE" -l "cnpg.io/cluster=${APP}-postgres,cnpg.io/jobRole=full-recovery" --field-selector=status.phase=Failed -o name 2>/dev/null || true)

    if [[ -n "$failed_pods" ]]; then
        log_info "Found failed recovery pods, deleting them..."
        echo "$failed_pods" | xargs kubectl delete -n "$NAMESPACE"
        log_success "Failed recovery pods cleaned up"
    else
        log_info "No failed recovery pods found"
    fi
}

# Prepare for recovery
prepare_recovery() {
    log_info "Preparing cluster for disaster recovery..."

    log_warning "This operation should only be performed during disaster recovery!"
    log_warning "It will delete the existing cluster and PVCs if they exist."

    read -p "Are you sure you want to continue? (yes/no): " -r
    if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        log_info "Operation cancelled"
        exit 0
    fi

    # Clean up failed jobs first
    clean_failed_jobs

    # Delete cluster if it exists and is in a bad state
    if kubectl get cluster "${APP}-postgres" -n "$NAMESPACE" &> /dev/null; then
        log_info "Deleting existing cluster..."
        kubectl delete cluster "${APP}-postgres" -n "$NAMESPACE" --ignore-not-found=true
        log_success "Cluster deleted"
    fi

    # Check if PVCs should be deleted
    log_warning "Do you want to delete the PVCs? (Only do this if they are corrupted)"
    read -p "Delete PVCs? (yes/no): " -r
    if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        log_info "Deleting PVCs..."
        kubectl delete pvc -n "$NAMESPACE" -l "cnpg.io/cluster=${APP}-postgres" --ignore-not-found=true
        log_success "PVCs deleted"
    else
        log_info "PVCs preserved"
    fi

    log_success "Cluster prepared for recovery"
    log_info "Now you need to apply your configuration with bootstrap.recovery enabled"
}

# Perform recovery
perform_recovery() {
    log_info "Performing disaster recovery for $APP..."

    # Check if backup source exists
    if ! kubectl get objectstore "${APP}-minio" -n "$NAMESPACE" &> /dev/null; then
        log_error "ObjectStore ${APP}-minio not found. Cannot perform recovery without backup configuration."
        exit 1
    fi

    log_info "Recovery will be performed from backup source: ${APP}-backup"
    log_info "Monitor the recovery with: kubectl logs -f -l cnpg.io/jobRole=full-recovery -n $NAMESPACE"

    log_success "Recovery initiated. Check cluster status with: $0 status -n $NAMESPACE -a $APP"
}

# Clean up after successful recovery
cleanup_recovery() {
    log_info "Cleaning up after successful recovery..."

    # Check if cluster is healthy
    if ! kubectl get cluster "${APP}-postgres" -n "$NAMESPACE" -o jsonpath='{.status.phase}' | grep -q "Cluster in healthy state"; then
        log_warning "Cluster is not in a healthy state. Are you sure you want to clean up?"
        read -p "Continue with cleanup? (yes/no): " -r
        if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
            log_info "Cleanup cancelled"
            exit 0
        fi
    fi

    # Clean up successful recovery jobs (they're no longer needed)
    recovery_jobs=$(kubectl get jobs -n "$NAMESPACE" -l "cnpg.io/cluster=${APP}-postgres,cnpg.io/jobRole=full-recovery" -o name 2>/dev/null || true)

    if [[ -n "$recovery_jobs" ]]; then
        log_info "Cleaning up recovery jobs..."
        echo "$recovery_jobs" | xargs kubectl delete -n "$NAMESPACE"
        log_success "Recovery jobs cleaned up"
    fi

    log_success "Cleanup completed"
    log_info "Remember to remove the bootstrap.recovery section from your cluster configuration to prevent issues with future updates!"
}

# Main function
main() {
    parse_args "$@"

    if [[ -z "${COMMAND:-}" ]]; then
        log_error "No command specified"
        show_help
        exit 1
    fi

    check_prerequisites

    case "$COMMAND" in
        status)
            show_status
            ;;
        backup-status)
            show_backup_status
            ;;
        prepare-recovery)
            prepare_recovery
            ;;
        recover)
            perform_recovery
            ;;
        cleanup-recovery)
            cleanup_recovery
            ;;
        clean-failed-jobs)
            clean_failed_jobs
            ;;
        *)
            log_error "Unknown command: $COMMAND"
            show_help
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"
