#!/opt/homebrew/bin/pwsh
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true, HelpMessage = "Application name for the replica")]
  [ValidateNotNullOrEmpty()]
  [string]$app,

  [Parameter(Mandatory = $true, HelpMessage = "Template name for the replica configuration")]
  [ValidateNotNullOrEmpty()]
  [string]$templateName,

  [Parameter(Mandatory = $true, HelpMessage = "Destination directory for output files")]
  [string]$destination,

  [Parameter(Mandatory = $true, HelpMessage = "Number of replicas to create")]
  [string]$replicas,

  [Parameter(Mandatory = $false, HelpMessage = "Access modes for persistent volumes")]
  [ValidateSet("ReadWriteOnce", "ReadWriteMany", "ReadOnlyMany")]
  [string]$volsyncAccessModes,

  [Parameter(Mandatory = $false, HelpMessage = "Storage capacity for the PVC")]
  [ValidatePattern("^\d+[KMGT]i?$")]
  [string]$volsyncCapacity,

  [Parameter(Mandatory = $false, HelpMessage = "Storage class for the main PVC")]
  [string]$volsyncStorageClass,

  [Parameter(Mandatory = $false, HelpMessage = "Copy method for replication")]
  [ValidateSet("Clone", "Snapshot")]
  [string]$volsyncCopyMethod,

  [Parameter(Mandatory = $false, HelpMessage = "Volume snapshot class")]
  [string]$volsyncSnapshotClass,

  [Parameter(Mandatory = $false, HelpMessage = "Cache capacity for ReplicationSource")]
  [ValidatePattern("^\d+[KMGT]i?$")]
  [string]$volsyncCacheCapacity,

  [Parameter(Mandatory = $false, HelpMessage = "Cache storage class")]
  [string]$volsyncCacheSnapshotClass,

  [Parameter(Mandatory = $false, HelpMessage = "Cache access modes")]
  [ValidateSet("ReadWriteOnce", "ReadWriteMany", "ReadOnlyMany")]
  [string]$volsyncCacheAccessModes,

  [Parameter(Mandatory = $false, HelpMessage = "User ID for mover security context")]
  [int]$volsyncPuid,

  [Parameter(Mandatory = $false, HelpMessage = "Group ID for mover security context")]
  [int]$volsyncPgid
)

# Create comprehensive token hashtable
$tokens = @{
  "APP" = "`${REPLICA}";
}

function replace-defaults ($content) {
  return $content -replace "\$\{(.*?)(?:\:\=(.*))?\}", {
    $varName = $_.Groups[1].Captures[0].Value
    $defaultValue = $_.Groups[2].Captures[0].Value
    Write-Host "Processing variable: $varName with default value: $defaultValue" -ForegroundColor Cyan

    # this code is sadly stateful, the desintation template must be loaded last.
    if ($varName -eq "VOLSYNC_CAPACITY") {
      # Handle the different cache capacity values for ReplicationSource
      $tokens['VOLSYNC_CACHE_CAPACITY'] = $defaultValue
      return "`${VOLSYNC_CACHE_CAPACITY}"
    }

    $tokens[$varName] = $tokens[$varName] ?? $defaultValue;
    return   "`${$varName}"
  }
}

function get-template ($path) {
  $path = Resolve-Path "$PSScriptRoot/../$path";
  return replace-defaults (get-content $path -raw)
}

function replace-values ($content, $key, $value) {
  write-host "Replacing `${$key} with $value" -ForegroundColor Cyan
  # Use regex escape for the pattern and ensure proper variable substitution
  return $content -replace "\`$\{$key\}", $value
}

$secretTemplate = get-template "kubernetes/components/volsync/local/externalsecret.yaml"
$replicationSourceTemplate = get-template "kubernetes/components/volsync/local/replicationsource.yaml"
$pvcTemplate = get-template "kubernetes/components/volsync/pvc.yaml"
$destinationTemplate = get-template "kubernetes/components/volsync/local/replicationdestination.yaml"

if (-not [string]::IsNullOrEmpty($volsyncAccessModes)) {
  $tokens['VOLSYNC_ACCESSMODES'] = $volsyncAccessModes
}
if (-not [string]::IsNullOrEmpty($volsyncCapacity)) {
  $tokens['VOLSYNC_CAPACITY'] = $volsyncCapacity
}
if (-not [string]::IsNullOrEmpty($volsyncStorageClass)) {
  $tokens['VOLSYNC_STORAGECLASS'] = $volsyncStorageClass
}
if (-not [string]::IsNullOrEmpty($volsyncCopyMethod)) {
  $tokens['VOLSYNC_COPYMETHOD'] = $volsyncCopyMethod
}
if (-not [string]::IsNullOrEmpty($volsyncSnapshotClass)) {
  $tokens['VOLSYNC_SNAPSHOTCLASS'] = $volsyncSnapshotClass
}
if (-not [string]::IsNullOrEmpty($volsyncCacheCapacity)) {
  $tokens['VOLSYNC_CACHE_CAPACITY'] = $volsyncCacheCapacity
}
if (-not [string]::IsNullOrEmpty($volsyncCacheSnapshotClass)) {
  $tokens['VOLSYNC_CACHE_SNAPSHOTCLASS'] = $volsyncCacheSnapshotClass
}
if (-not [string]::IsNullOrEmpty($volsyncCacheAccessModes)) {
  $tokens['VOLSYNC_CACHE_ACCESSMODES'] = $volsyncCacheAccessModes
}
if (-not [string]::IsNullOrEmpty($volsyncPuid)) {
  $tokens['VOLSYNC_PUID'] = $volsyncPuid
}
if (-not [string]::IsNullOrEmpty($volsyncPgid)) {
  $tokens['VOLSYNC_PGID'] = $volsyncPgid
}

$destinationTemplate = replace-values $destinationTemplate 'VOLSYNC_CAPACITY' $volsyncCacheCapacityDestination

$template = @"
$($pvcTemplate)
$($secretTemplate)
$($destinationTemplate)
$($replicationSourceTemplate)
"@;


foreach ($key in $tokens.Keys) {
  $value = $tokens[$key]
  # Replace tokens with their corresponding values
  $template = replace-values $template $key $value
}

# Write output file
for ($i = 0; $i -lt $replicas; $i++) {
  $outputPath = Join-Path $destination "replica-$i.yaml"
  Set-Content -Path $outputPath -Value (replace-values $template "REPLICA" "$templateName-$app-${i}")
}
Write-Host "Replica files created in $destination" -ForegroundColor Green
