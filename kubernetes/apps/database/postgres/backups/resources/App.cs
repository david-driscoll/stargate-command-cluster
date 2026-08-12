#!/usr/bin/dotnet run
// #:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package KubernetesClient@*
#:package Microsoft.Extensions.Logging@10.*
#:package Lunet.Extensions.Logging.SpectreConsole@1.2.0
#:package ProcessX@1.5.6
#:package Npgsql@*
#:property JsonSerializerIsReflectionEnabledByDefault=true

// Postgres backup. Credentials come from the Kubernetes Secrets in THIS
// namespace -- the same objects the ExternalSecrets in
// kubernetes/apps/database/postgres/app/users.yaml render from the sops-held
// per-app passwords.
//
// This used to read them back out of 1Password, where a PushSecret had pushed
// these very Secrets 24h earlier (Phase 10 of the 1Password->OpenBao
// migration; vault repo docs/openbao-migration/PLAN.md SS-G row 10). That was a
// round trip out of the cluster and back into the same namespace, and it cost
// more than an indirection: the copy was up to a refreshInterval stale, and a
// database whose PushSecret had been removed kept being backed up from a
// FROZEN item -- invisibly, because a stale credential still authenticates.
// Reading the Secret directly makes a missing credential an error at the
// moment it goes missing.

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using k8s;
using Npgsql;
using File = System.IO.File;

var config = KubernetesClientConfiguration.InClusterConfig();
using var kubernetes = new Kubernetes(config);

// The Secrets live alongside this CronJob. Downward-API first so the value is
// explicit in the manifest; the service-account namespace file is the fallback.
var secretNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE")
  ?? config.Namespace
  ?? throw new InvalidOperationException("POD_NAMESPACE is not set and no in-cluster namespace could be determined");

async Task<IDictionary<string, byte[]>> GetSecret(string name)
{
  try
  {
    var secret = await kubernetes.CoreV1.ReadNamespacedSecretAsync(name, secretNamespace);
    return secret.Data ?? throw new InvalidOperationException($"secret {secretNamespace}/{name} has no data");
  }
  catch (k8s.Autorest.HttpOperationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
  {
    // Named explicitly: this is what a database with no matching credential
    // Secret looks like, and it is the case the 1Password round trip used to
    // hide behind a stale copy.
    throw new InvalidOperationException($"secret {secretNamespace}/{name} does not exist -- the database has no credential Secret in this cluster", ex);
  }
}

static string GetField(IDictionary<string, byte[]> secret, string name, string key) =>
  secret.TryGetValue(key, out var value)
    ? Encoding.UTF8.GetString(value)
    : throw new InvalidOperationException($"{key} key not found in secret {name}");

// Databases whose application has been decommissioned but whose data is still
// in postgres. They have no credential Secret, so without this they would fail
// the run. The list is per-cluster config, set in cronjob.yaml, where each name
// carries the date its application was removed.
//
// Deliberately an explicit allowlist and never a "skip anything with no Secret"
// fallback: a missing Secret is exactly how a LIVE database silently drops out
// of the backup set, and making that loud is the point of this phase.
var decommissioned = (Environment.GetEnvironmentVariable("DECOMMISSIONED_DATABASES") ?? "")
  .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
  .ToHashSet(StringComparer.OrdinalIgnoreCase);

var backupDir = "/backups";

Console.WriteLine($"Starting PostgreSQL backup at {DateTime.UtcNow}");

// Create backup directory
Directory.CreateDirectory(backupDir);

List<string> databases;
{
  // Get list of databases
  var postgres = await GetSecret("postgres-user");
  // NEVER log this -- the connection string embeds the plaintext password and
  // pod stdout is shipped to Loki.
  var connectionString = GetField(postgres, "postgres-user", "connection-string");
  Console.WriteLine("Fetching list of databases...");
  await using var dataSource = NpgsqlDataSource.Create(connectionString);
  databases = await GetDatabases(dataSource);
  Console.WriteLine($"Found databases: {string.Join(", ", databases)}");
}

// A listed database that no longer exists means the entry outlived its
// database -- harmless, but it rots, so say so rather than letting the list
// grow stale silently.
var staleEntries = decommissioned.Except(databases, StringComparer.OrdinalIgnoreCase).ToList();
if (staleEntries.Count > 0)
{
  Console.WriteLine($"Note: DECOMMISSIONED_DATABASES lists {string.Join(", ", staleEntries)}, which no longer exist in postgres -- remove the entries");
}

// Create individual database dumps
var failed = new List<string>();
var skipped = new List<string>();
foreach (var db in databases)
{
  if (decommissioned.Contains(db))
  {
    // Printed every run, by name: a database leaving the backup set should be
    // visible in the log of the job that stopped backing it up.
    Console.WriteLine($"SKIPPING {db}: decommissioned, not backed up (DECOMMISSIONED_DATABASES)");
    skipped.Add(db);
    continue;
  }

  var backupFile = Path.Combine(backupDir, $"{db}.sql.gz");
  // Dump to a sibling temp file and only replace the existing backup once pg_dump
  // has exited 0. Writing straight to backupFile truncates the last known-good
  // dump before the new one is known to be valid.
  var stagingFile = $"{backupFile}.tmp";
  try
  {
    var secretName = $"{db}-postgres";
    var postgres = await GetSecret(secretName);

    // Host/port/user only -- never the password or the full connection string.
    Console.WriteLine($"Backing up database: {db} ({GetField(postgres, secretName, "username")}@{GetField(postgres, secretName, "hostname")}:{GetField(postgres, secretName, "port")})");
    Directory.CreateDirectory(Path.GetDirectoryName(backupFile) ?? throw new InvalidOperationException("Failed to get directory name for backup file"));

    await CreateDatabaseDump(postgres, secretName, db, stagingFile);
    File.Move(stagingFile, backupFile, overwrite: true);

    if (File.Exists(backupFile))
    {
      Console.WriteLine($"Successfully created backup: {backupFile}");
    }
    else
    {
      Console.Error.WriteLine($"Failed to create backup for database: {db}");
      failed.Add(db);
    }
  }
  catch (Exception ex)
  {
    Console.Error.WriteLine($"Error backing up database {db}: {ex.Message}");
    failed.Add(db);
  }
  finally
  {
    if (File.Exists(stagingFile)) File.Delete(stagingFile);
  }
}

if (failed.Count > 0)
{
  Console.Error.WriteLine($"PostgreSQL backup FAILED at {DateTime.UtcNow}: {failed.Count} of {databases.Count - skipped.Count} database(s) were not backed up: {string.Join(", ", failed)}");
  return 1;
}

Console.WriteLine($"PostgreSQL backup completed successfully at {DateTime.UtcNow} ({databases.Count - skipped.Count} databases{(skipped.Count > 0 ? $", {skipped.Count} skipped: {string.Join(", ", skipped)}" : "")})");
return 0;

// Helper methods
async Task<List<string>> GetDatabases(NpgsqlDataSource dataSource)
{
  var databases = new List<string>();
  await using var connection = await dataSource.OpenConnectionAsync();
  using var command = connection.CreateCommand();
  command.CommandText = "SELECT datname FROM pg_database WHERE datistemplate = false;";
  await using var reader = await command.ExecuteReaderAsync();
  while (await reader.ReadAsync())
  {
    if (reader.GetString(0) is "postgres" or "app") continue;
    databases.Add(reader.GetString(0));
  }

  return databases;
}

async Task CreateDatabaseDump(IDictionary<string, byte[]> postgres, string secretName, string database, string outputFile)
{
  var host = GetField(postgres, secretName, "hostname");
  var port = GetField(postgres, secretName, "port");
  var user = GetField(postgres, secretName, "username");
  var password = GetField(postgres, secretName, "password");
  var psi = new ProcessStartInfo
  {
    FileName = "pg_dump",
    Arguments = $"-h {host} -p {port} -U {user} -d {database} --verbose --no-password --format=custom --no-privileges --no-owner",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true,
  };
  psi.Environment["PGPASSWORD"] = password;

  using var process = Process.Start(psi);
  if (process == null) throw new InvalidOperationException("Failed to start pg_dump process");

  // --verbose writes progress to stderr throughout the dump. Drain it concurrently
  // with stdout: reading it only after the process exits deadlocks once the stderr
  // pipe buffer fills, because pg_dump then blocks before it can finish writing stdout.
  var errorTask = process.StandardError.ReadToEndAsync();

  // Compress the output
  await using (var fileStream = File.Create(outputFile))
  await using (var gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
  {
    await process.StandardOutput.BaseStream.CopyToAsync(gzipStream);
  }

  await process.WaitForExitAsync();
  var error = await errorTask;

  if (process.ExitCode != 0)
  {
    throw new InvalidOperationException($"pg_dump failed: {error}");
  }
}
