#!/usr/bin/dotnet run
// #:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package KubernetesClient@*
#:package Microsoft.Extensions.Logging@10.*
#:package Lunet.Extensions.Logging.SpectreConsole@1.2.0
#:package ProcessX@1.5.6
#:package 1Password.Connect.Sdk@1.0.4
#:package Npgsql@*
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Npgsql;
using OnePassword.Connect.Sdk;
using OnePassword.Connect.Sdk.Models;
using File = System.IO.File;

var opClient = new OnePasswordConnectClient(new OnePasswordConnectOptions()
{
  BaseUrl = Environment.GetEnvironmentVariable("CONNECT_HOST") ?? throw new InvalidOperationException("CONNECT_HOST is required"),
  ApiKey = Environment.GetEnvironmentVariable("CONNECT_TOKEN") ?? throw new InvalidOperationException("CONNECT_TOKEN is required"),
});

var vaultId = (await opClient.GetVaultsAsync("")).Single(z => z.Name == "Eris").Id ?? throw new InvalidOperationException("Eris vault not found");

async Task<FullItem> GetItemByTitle(string title)
{
  var items = await opClient.GetVaultItemsAsync(vaultId, $"title eq \"{title}\"");
  return await opClient.GetVaultItemByIdAsync(vaultId, (items.SingleOrDefault(i => i.Title == title) ?? throw new InvalidOperationException($"{title} item not found")).Id);
}
static string GetField(FullItem item, string label) => item.Fields.Single(f => f.Label == label).Value ?? throw new InvalidOperationException($"{label} field not found in {item.Title}");
var backblaze = await GetItemByTitle("Backblaze S3 ${CLUSTER_TITLE} Database");

var backupDir = "/backups";

Console.WriteLine($"Starting PostgreSQL backup at {DateTime.UtcNow}");

const string clusterKey = "${CLUSTER_CNAME}";
// Create backup directory
Directory.CreateDirectory(backupDir);

List<string> databases;
{
  // Get list of databases
  var postgres = await GetItemByTitle("${CLUSTER_CNAME}-postgres-user");
  // NEVER log this — the connection string embeds the plaintext password and
  // pod stdout is shipped to Loki.
  var connectionString = GetField(postgres, "connection-string");
  Console.WriteLine("Fetching list of databases...");
  await using var dataSource = NpgsqlDataSource.Create(connectionString);
  databases = await GetDatabases(dataSource);
  Console.WriteLine($"Found databases: {string.Join(", ", databases)}");
}

// Create individual database dumps
var failed = new List<string>();
foreach (var db in databases)
{
  var backupFile = Path.Combine(backupDir, $"{db}.sql.gz");
  // Dump to a sibling temp file and only replace the existing backup once pg_dump
  // has exited 0. Writing straight to backupFile truncates the last known-good
  // dump before the new one is known to be valid.
  var stagingFile = $"{backupFile}.tmp";
  try
  {
    var postgres = await GetItemByTitle($"{clusterKey}-{db}-postgres");

    // Host/port/user only — never the password or the full connection string.
    Console.WriteLine($"Backing up database: {db} ({GetField(postgres, "username")}@{GetField(postgres, "hostname")}:{GetField(postgres, "port")})");
    Directory.CreateDirectory(Path.GetDirectoryName(backupFile) ?? throw new InvalidOperationException("Failed to get directory name for backup file"));

    await CreateDatabaseDump(postgres, db, stagingFile);
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
  Console.Error.WriteLine($"PostgreSQL backup FAILED at {DateTime.UtcNow}: {failed.Count} of {databases.Count} database(s) were not backed up: {string.Join(", ", failed)}");
  return 1;
}

Console.WriteLine($"PostgreSQL backup completed successfully at {DateTime.UtcNow} ({databases.Count} databases)");
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

async Task CreateDatabaseDump(FullItem postgres, string database, string outputFile)
{
  var host = GetField(postgres, "hostname");
  var port = GetField(postgres, "port");
  var user = GetField(postgres, "username");
  var password = GetField(postgres, "password");
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
