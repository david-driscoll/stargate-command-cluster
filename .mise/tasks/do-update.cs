#!/usr/bin/dotnet run

#:package ProcessX@1.5.6

using Cysharp.Diagnostics;

foreach (var item in Directory.EnumerateFiles("kubernetes", "Update.cs", SearchOption.AllDirectories))
{
  Console.WriteLine($"Processing: {item}");
  var (process, stdout, stderr) = ProcessX.GetDualAsyncEnumerable($"dotnet run {item}", workingDirectory: Directory.GetCurrentDirectory());
  await foreach (var line in stdout)
  {
    Console.WriteLine(line);
  }
  await foreach (var line in stderr)
  {
    Console.Error.WriteLine(line);
  }
}

