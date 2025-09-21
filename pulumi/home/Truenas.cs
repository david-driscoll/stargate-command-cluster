using System.Linq;
using Dumpify;
using models;
using Pulumi;
using Pulumi.Truenas;
using Pulumiverse.Purrl;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Provider = Pulumi.Truenas.Provider;

namespace home;

public class Truenas : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required GlobalResources Globals { get; init; }
    public required ProxmoxHost Host { get; init; }
    public Input<string>? VmName { get; init; } = null;
  }

  public Truenas(string name, Args args, ComponentResourceOptions? options = null) : base("custom:resource:truenas",
    name, args, options)
  {
    var invokeOptions = new InvokeOptions() { Provider = args.Globals.TruenasProvider, Parent = this };
    var cro = new CustomResourceOptions() { Provider = args.Globals.TruenasProvider, Parent = this };
    BackupDataset = GetDataset.Invoke(new() { DatasetId = BackupDatasetId, }, invokeOptions);
    DataDataset = GetDataset.Invoke(new() { DatasetId = DataDatasetId, }, invokeOptions);
    MediaDataset = GetDataset.Invoke(new() { DatasetId = MediaDatasetId, }, invokeOptions);
    Provider = args.Globals.TruenasProvider;
    Credential = args.Globals.TruenasCredential;
  }

  public Output<GetAPICredentialResult> Credential { get; }
  public string MediaDatasetId => "stash/backup";
  public string DataDatasetId => "stash/data";
  public string BackupDatasetId => "stash/media";
  public Output<GetDatasetResult> MediaDataset { get; }
  public Output<GetDatasetResult> DataDataset { get; }
  public Output<GetDatasetResult> BackupDataset { get; }
  public Provider Provider { get; }

  public ClusterBackup AddClusterBackup(string name, bool import = true)
  {
    return new ClusterBackup(name, new()
    {
      Truenas = this,
      Import = import,
    });
  }
}

public class ClusterBackup : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required Truenas Truenas { get; init; }
    public required bool Import { get; set; }
  }

  public ClusterBackup(string name, Args args, ComponentResourceOptions? options = null) : base(
    "custom:truenas:cluster-backup", name, args, options)
  {
    var cro = new CustomResourceOptions() { Provider = args.Truenas.Provider, Parent = this };
    var ccro = new ComponentResourceOptions() { Parent = this };
    var container = new Dataset(name, new()
      {
        Pool = args.Truenas.BackupDataset.Apply(z => z.Pool),
        Parent = args.Truenas.BackupDataset.Apply(z => string.Join("/", z.Id.Split('/').Skip(1)).Dump()),
        Name = name,
      },
      CustomResourceOptions.Merge(cro,
        new()
        {
          ImportId = args.Import ? $"{args.Truenas.BackupDatasetId}/{name}" : null,
          RetainOnDelete = true
        }));

    LonghornDataset = new Dataset($"{name}-longhorn-dataset", new()
    {
      Pool = container.Pool,
      Parent = container.Pool.Apply(z => string.Join("/", z.Split('/').Skip(1)).Dump()),
      Name = "longhorn",
    }, CustomResourceOptions.Merge(cro,
      new()
      {
        ImportId = args.Import ? $"{args.Truenas.BackupDatasetId}/{name}/longhorn" : null,
        RetainOnDelete = true
      }));

    VolsyncDataset = new Dataset($"{name}-volsync-dataset", new()
    {
      Pool = container.Pool,
      Parent = container.Pool.Apply(z => string.Join("/", z.Split('/').Skip(1)).Dump()),
      Name = "volsync",
    }, CustomResourceOptions.Merge(cro,
      new()
      {
        ImportId = args.Import ? $"{args.Truenas.BackupDatasetId}/{name}/volsync" : null,
        RetainOnDelete = true
      }));

    LonghornNfs = new TruenasNfsShare($"{name}-longhorn-nfs", new()
    {
      Truenas = args.Truenas,
      Path = LonghornDataset.MountPoint,
      MapallUser = "apps",
      MapallGroup = "apps",
    }, ccro);

    VolsyncNfs = new TruenasNfsShare($"{name}-volsync-nfs", new()
    {
      Truenas = args.Truenas,
      Path = VolsyncDataset.MountPoint,
      MapallUser = "apps",
      MapallGroup = "apps",
    }, ccro);
  }

  public TruenasNfsShare VolsyncNfs { get; set; }
  public TruenasNfsShare LonghornNfs { get; set; }
  public Dataset VolsyncDataset { get; set; }
  public Dataset LonghornDataset { get; set; }
}

public class TruenasNfsShare : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required Truenas Truenas { get; init; }
    public required Input<string> Path { get; init; }
    public Input<string>? MapallGroup { get; set; }
    public Input<string>? MapallUser { get; set; }
    public Input<string>? MaprootGroup { get; set; }
    public Input<string>? MaprootUser { get; set; }
    public Input<string>? Comment { get; set; }
  }

  public TruenasNfsShare(string name, Args args, ComponentResourceOptions? options = null) : base(
    "custom:truenas:nfs-share", name, args, options)
  {
    var cro = new CustomResourceOptions() { Parent = this };
    _ = new Purrl(name, new PurrlArgs()
    {
      Name = name,
      Method = "POST",
      Url = Output.Format($"https://{args.Truenas.Credential.Apply(z => z.Fields["domain"].Value)}/api/v2.0"),
      Headers = new InputMap<string>()
      {
        ["Authorization"] = args.Truenas.Credential.Apply(z => $"Bearer {z.Credential}"),
      },
      ResponseCodes = ["200"],
      Body = Output.JsonSerialize(Output.Create(new
      {
        mapall_group = args.MapallGroup,
        mapall_user = args.MapallUser,
        maproot_group = args.MaprootGroup,
        maproot_user = args.MaprootUser,
        path = args.Path,
        comment = args.Comment
      })),
    }, cro);
  }
}
