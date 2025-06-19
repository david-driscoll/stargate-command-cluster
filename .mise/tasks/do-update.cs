#!/usr/bin/dotnet run

#:package ProcessX@1.5.6

using Cysharp.Diagnostics;

foreach (var item in Directory.EnumerateFiles("kubernetes", "Update.cs", SearchOption.AllDirectories))
{
  await ProcessX.StartAsync($"dotnet run {item}").WaitAsync();
}

