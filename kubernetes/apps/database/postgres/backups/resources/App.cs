#!/usr/bin/dotnet run
// #:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package KubernetesClient@*
#:package Microsoft.Extensions.Logging@9.*
#:package Backblaze.Client@1.*
#:package Dumpify@0.6.6
#:package Lunet.Extensions.Logging.SpectreConsole@1.2.0
#:package ProcessX@1.5.6
#:package 1Password.Connect.Sdk@1.0.4
#:package Npgsql@*
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;
using Bytewizer.Backblaze.Progress;
using Dumpify;
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
var postgres = await GetItemByTitle("${CLUSTER_KEY}-postgres-superuser");
var connectionString = postgres.Fields.Single(f => f.Label == "public-connection-string").Value.Dump();

var backupDir = "/tmp/backups";
var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

Console.WriteLine($"Starting PostgreSQL backup at {DateTime.UtcNow}");

// Create backup directory
Directory.CreateDirectory(backupDir);

// Initialize Backblaze client
var backblazeClient = BackblazeClient.Initialize(GetField(backblaze, "username"), GetField(backblaze, "credential"));

await using var dataSource = NpgsqlDataSource.Create(connectionString);

// Get list of databases
Console.WriteLine("Fetching list of databases...");
var databases = await GetDatabases(dataSource);
Console.WriteLine($"Found databases: {string.Join(", ", databases)}");

// Create individual database dumps
foreach (var db in databases)
{
  Console.WriteLine($"Backing up database: {db}");
  var backupFile = Path.Combine(backupDir, $"{db}_{timestamp}.sql.gz");

  await CreateDatabaseDump(postgres, db, backupFile);

  if (File.Exists(backupFile))
  {
    Console.WriteLine($"Successfully created backup: {backupFile}");

    // Upload to Backblaze
    Console.WriteLine($"Uploading {backupFile} to Backblaze...");
    var fileName = $"dumps/{db}/{Path.GetFileName(backupFile)}";
    await UploadFile(backblazeClient, GetField(backblaze, "bucket"), backupFile, fileName);

    Console.WriteLine($"Successfully uploaded {backupFile} to Backblaze");
    File.Delete(backupFile);
  }
  else
  {
    Console.WriteLine($"Failed to create backup for database: {db}");
    Environment.Exit(1);
  }
}

// Cleanup old backups (keep last 30 days)
Console.WriteLine("Cleaning up old backups...");
await CleanupOldBackups(backblazeClient, GetField(backblaze, "bucket"));

Console.WriteLine($"PostgreSQL backup completed successfully at {DateTime.UtcNow}");

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
  var host = GetField(postgres, "public-hostname");
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

  // Compress the output
  using var fileStream = File.Create(outputFile);
  using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);

  await process.StandardOutput.BaseStream.CopyToAsync(gzipStream);
  await process.WaitForExitAsync();

  if (process.ExitCode != 0)
  {
    var error = await process.StandardError.ReadToEndAsync();
    throw new InvalidOperationException($"pg_dump failed: {error}");
  }
}

async Task UploadFile(BackblazeClient client, string bucketId, string localFilePath, string fileName)
{
  using var fileStream = File.OpenRead(localFilePath);
  var uploadUrlResponse = await client.Files.GetUploadUrlAsync(bucketId);

  var progress = new NaiveProgress<ICopyProgress>();

  var uploadResponse = await client.Files.UploadAsync(
    bucketId,
    fileName,
    localFilePath,
    progress,
    CancellationToken.None
  );
  uploadResponse.Dump("Upload Response");
  if (!uploadResponse.HttpResponse.IsSuccessStatusCode)
  {
    throw new InvalidOperationException($"Failed to upload file to Backblaze: {uploadResponse.Error.Message}");
  }

}

async Task CleanupOldBackups(BackblazeClient client, string bucketId)
{
  var cutoffDate = DateTime.UtcNow.AddDays(-30);
  // List files in the dumps folder
  var listResponse = await client.Files.GetEnumerableAsync(new ListFileNamesRequest(bucketId) { Prefix = "dumps/" });
  if (listResponse?.ToList() is not { Count: > 0 }) return;

  foreach (var file in listResponse)
  {
    if (file.UploadTimestamp >= cutoffDate) continue;
    Console.WriteLine($"Deleting old backup: {file.FileName}");
    await client.Files.DeleteAsync(file.FileId, file.FileName);
  }
}
