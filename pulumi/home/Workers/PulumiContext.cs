using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using k8s;
using Microsoft.EntityFrameworkCore;
using Pulumi.Automation;

namespace models.Workers;

public class PulumiContext : DbContext
{
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder
      .Entity<AuthentikStack>()
      .OwnsOne(c => c.Settings, d => d.ToJson())
      .OwnsOne(c => c.Outputs, d => d.ToJson())
      ;
  }

  public DbSet<AuthentikStack> Applications { get; set; }
}

public class AuthentikStack
{
  [Key] public string Name { get; set; } = null!;
  public Dictionary<string, OutputValue> Outputs { get; set; } = new();
  public StackSettings Settings { get; set; } = new();
}

public class ClusterDefinitionClient(IKubernetes kubernetes, bool disposeClient = false)
  : GenericClient(kubernetes, "driscoll.dev", "v1", "clusterdefinitions", disposeClient);
public class ApplicationDefinitionClient(IKubernetes kubernetes, bool disposeClient = false)
  : GenericClient(kubernetes, "driscoll.dev", "v1", "applicationdefinitions", disposeClient);
