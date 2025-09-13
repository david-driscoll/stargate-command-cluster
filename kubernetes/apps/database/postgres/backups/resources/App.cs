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
using Dumpify;
using Npgsql;
using OnePassword.Connect.Sdk;
using OnePassword.Connect.Sdk.Models;

var opClient = new OnePasswordConnectClient(new OnePasswordConnectOptions()
{
  ApiKey = Environment.GetEnvironmentVariable("CONNECT_TOKEN") ?? throw new InvalidOperationException("CONNECT_TOKEN is required"),
});
opClient.BaseUrl = Environment.GetEnvironmentVariable("CONNECT_HOST") ?? throw new InvalidOperationException("CONNECT_HOST is required");

async Task<FullItem> GetItemById(string title)
{
  var items = await opClient.GetVaultItemsAsync("Eris", $"title eq \"{title}\"");
  return await opClient.GetVaultItemByIdAsync("Eris", (items.SingleOrDefault(i => i.Title == title) ?? throw new InvalidOperationException($"{title} item not found")).Id);
}
static string GetField(FullItem item, string label) => item.Fields.Single(f => f.Label == label).Value ?? throw new InvalidOperationException($"{label} field not found in {item.Title}");
var backblaze = await GetItemById("backblaze-b2-credentials");
var postgres = await GetItemById("equestria-postgres-superuser");
var connectionString = postgres.Fields.Single(f => f.Label == "public-connection-string").Value;
var s3Bucket = "equestria-db";

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

// // Create individual database dumps
// foreach (var db in databases)
// {
//   Console.WriteLine($"Backing up database: {db}");
//   var backupFile = Path.Combine(backupDir, $"{db}_{timestamp}.sql.gz");

//   await CreateDatabaseDump(postgresHost, postgresPort, postgresUser, postgresPassword, db, backupFile);

//   if (File.Exists(backupFile))
//   {
//     Console.WriteLine($"Successfully created backup: {backupFile}");

//     // Upload to Backblaze
//     Console.WriteLine($"Uploading {backupFile} to Backblaze...");
//     var fileName = $"database-dumps/{db}/{Path.GetFileName(backupFile)}";
//     await UploadFile(backblazeClient, bucket.BucketId, backupFile, fileName);

//     Console.WriteLine($"Successfully uploaded {backupFile} to Backblaze");
//     File.Delete(backupFile);
//   }
//   else
//   {
//     Console.WriteLine($"Failed to create backup for database: {db}");
//     Environment.Exit(1);
//   }
// }

// // Create global dump
// Console.WriteLine("Creating global database dump...");
// var globalBackupFile = Path.Combine(backupDir, $"postgres_all_{timestamp}.sql.gz");

// await CreateGlobalDump(postgresHost, postgresPort, postgresUser, postgresPassword, globalBackupFile);

// if (File.Exists(globalBackupFile))
// {
//   Console.WriteLine($"Successfully created global backup: {globalBackupFile}");

//   // Upload global backup to Backblaze
//   Console.WriteLine($"Uploading {globalBackupFile} to Backblaze...");
//   var fileName = $"database-dumps/global/{Path.GetFileName(globalBackupFile)}";
//   await UploadFile(backblazeClient, bucket.BucketId, globalBackupFile, fileName);

//   Console.WriteLine($"Successfully uploaded {globalBackupFile} to Backblaze");
//   File.Delete(globalBackupFile);
// }
// else
// {
//   Console.WriteLine("Failed to create global backup");
//   Environment.Exit(1);
// }

// // Cleanup old backups (keep last 30 days)
// Console.WriteLine("Cleaning up old backups...");
// await CleanupOldBackups(backblazeClient, bucket.BucketId);

// Console.WriteLine($"PostgreSQL backup completed successfully at {DateTime.UtcNow}");

// Helper methods
async Task<List<string>> GetDatabases(NpgsqlDataSource dataSource)
{
  var databases = new List<string>();
  await using (var connection = await dataSource.OpenConnectionAsync())
  {
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT datname FROM pg_database WHERE datistemplate = false AND datname NOT IN ('postgres');";
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      databases.Add(reader.GetString(0));
    }
  }

  return databases;
}

// async Task CreateDatabaseDump(string host, string port, string user, string password, string database, string outputFile)
// {
//   var psi = new ProcessStartInfo
//   {
//     FileName = "pg_dump",
//     Arguments = $"-h {host} -p {port} -U {user} -d {database} --verbose --no-password --format=custom --no-privileges --no-owner",
//     UseShellExecute = false,
//     RedirectStandardOutput = true,
//     RedirectStandardError = true,
//     CreateNoWindow = true
//   };
//   psi.Environment["PGPASSWORD"] = password;

//   using var process = Process.Start(psi);
//   if (process == null) throw new InvalidOperationException("Failed to start pg_dump process");

//   // Compress the output
//   using var fileStream = File.Create(outputFile);
//   using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);

//   await process.StandardOutput.BaseStream.CopyToAsync(gzipStream);
//   await process.WaitForExitAsync();

//   if (process.ExitCode != 0)
//   {
//     var error = await process.StandardError.ReadToEndAsync();
//     throw new InvalidOperationException($"pg_dump failed: {error}");
//   }
// }

// async Task CreateGlobalDump(string host, string port, string user, string password, string outputFile)
// {
//   var psi = new ProcessStartInfo
//   {
//     FileName = "pg_dumpall",
//     Arguments = $"-h {host} -p {port} -U {user} --verbose --no-password",
//     UseShellExecute = false,
//     RedirectStandardOutput = true,
//     RedirectStandardError = true,
//     CreateNoWindow = true
//   };
//   psi.Environment["PGPASSWORD"] = password;

//   using var process = Process.Start(psi);
//   if (process == null) throw new InvalidOperationException("Failed to start pg_dumpall process");

//   // Compress the output
//   using var fileStream = File.Create(outputFile);
//   using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);

//   await process.StandardOutput.BaseStream.CopyToAsync(gzipStream);
//   await process.WaitForExitAsync();

//   if (process.ExitCode != 0)
//   {
//     var error = await process.StandardError.ReadToEndAsync();
//     throw new InvalidOperationException($"pg_dumpall failed: {error}");
//   }
// }

// async Task UploadFile(BackblazeClient client, string bucketId, string localFilePath, string fileName)
// {
//   using var fileStream = File.OpenRead(localFilePath);

//   var uploadUrlResponse = await client.Files.GetUploadUrl(bucketId);

//   var uploadResponse = await client.Files.Upload(
//       uploadUrlResponse.UploadUrl,
//       uploadUrlResponse.AuthorizationToken,
//       fileName,
//       fileStream,
//       "application/gzip"
//   );

//   psi.Environment["PGPASSWORD"] = password;

//   using var process = Process.Start(psi);
//   if (process == null) throw new InvalidOperationException("Failed to start psql process");

//   var output = await process.StandardOutput.ReadToEndAsync();
//   await process.WaitForExitAsync();

//   if (process.ExitCode != 0)
//   {
//     var error = await process.StandardError.ReadToEndAsync();
//     throw new InvalidOperationException($"Failed to get database list: {error}");
//   }

//   var databases = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
//       .Select(db => db.Trim())
//       .Where(db => !string.IsNullOrWhiteSpace(db))
//       .ToList();

//   // Add postgres database
//   databases.Insert(0, "postgres");

//   return databases;
// }

// async Task CreateDatabaseDump(string host, string port, string user, string password, string database, string outputFile)
// {
//   var psi = new ProcessStartInfo
//   {
//     FileName = "pg_dump",
//     Arguments = $"-h {host} -p {port} -U {user} -d {database} --verbose --no-password --format=custom --no-privileges --no-owner",
//     UseShellExecute = false,
//     RedirectStandardOutput = true,
//     RedirectStandardError = true,
//     CreateNoWindow = true
//   };
//   psi.Environment["PGPASSWORD"] = password;

//   using var process = Process.Start(psi);
//   if (process == null) throw new InvalidOperationException("Failed to start pg_dump process");

//   // Compress the output
//   using var fileStream = File.Create(outputFile);
//   using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);

//   await process.StandardOutput.BaseStream.CopyToAsync(gzipStream);
//   await process.WaitForExitAsync();

//   if (process.ExitCode != 0)
//   {
//     var error = await process.StandardError.ReadToEndAsync();
//     throw new InvalidOperationException($"pg_dump failed: {error}");
//   }
// }

// async Task CreateGlobalDump(string host, string port, string user, string password, string outputFile)
// {
//   var psi = new ProcessStartInfo
//   {
//     FileName = "pg_dumpall",
//     Arguments = $"-h {host} -p {port} -U {user} --verbose --no-password",
//     UseShellExecute = false,
//     RedirectStandardOutput = true,
//     RedirectStandardError = true,
//     CreateNoWindow = true
//   };
//   psi.Environment["PGPASSWORD"] = password;

//   using var process = Process.Start(psi);
//   if (process == null) throw new InvalidOperationException("Failed to start pg_dumpall process");

//   // Compress the output
//   using var fileStream = File.Create(outputFile);
//   using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);

//   await process.StandardOutput.BaseStream.CopyToAsync(gzipStream);
//   await process.WaitForExitAsync();

//   if (process.ExitCode != 0)
//   {
//     var error = await process.StandardError.ReadToEndAsync();
//     throw new InvalidOperationException($"pg_dumpall failed: {error}");
//   }
// }

// async Task UploadFile(BackblazeClient client, string bucketId, string localFilePath, string fileName)
// {
//   using var fileStream = File.OpenRead(localFilePath);

//   var uploadUrlResponse = await client.Files.GetUploadUrl(bucketId);

//   var uploadResponse = await client.Files.Upload(
//       uploadUrlResponse.UploadUrl,
//       uploadUrlResponse.AuthorizationToken,
//       fileName,
//       fileStream,
//       "application/gzip"
//   );

//   if (uploadResponse == null)
//   {
//     throw new InvalidOperationException($"Failed to upload {fileName}");
//   }
// }

// async Task CleanupOldBackups(BackblazeClient client, string bucketId)
// {
//   var cutoffDate = DateTime.UtcNow.AddDays(-30);

//   // List files in the database-dumps folder
//   var listResponse = await client.Files.GetList(bucketId, startFileName: "database-dumps/", maxFileCount: 10000);

//   foreach (var file in listResponse.Files)
//   {
//     if (file.UploadTimestamp < cutoffDate)
//     {
//       Console.WriteLine($"Deleting old backup: {file.FileName}");
//       await client.Files.Delete(file.FileId, file.FileName);
//     }
//   }
// }
