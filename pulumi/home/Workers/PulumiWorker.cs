// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Collections.Immutable;
// using System.IO;
// using System.Linq;
// using System.Reactive.Disposables;
// using System.Reactive.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Threading.Tasks.Sources;
// using Dumpify;
// using DynamicData;
// using k8s;
// using k8s.Models;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using models;
// using models.Applications;
// using Pulumi.Automation;
//
// namespace models.Workers;
//
// public class PulumiWorker(ILogger<PulumiWorker> logger, PulumiContext context) : BackgroundService
// {
//   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//   {
//     var stacks = await context.Applications.ToArrayAsync(cancellationToken: stoppingToken);
//     var workspace = await LocalWorkspace.CreateAsync(new LocalWorkspaceOptions()
//     {
//       Logger = logger,
//       StackSettings = stacks.ToDictionary(z => z.Name, z => z.Settings),
//     }, stoppingToken);
//
//     // workspace == behavior / meta component
//     // stack == component within
//     //
//   }
// }
//
// public static class AuthentikApplications
// {
//   public class AuthentikApplicationsWorker(
//     ILogger<AuthentikApplicationsWorker> logger,
//     PulumiContext context,
//     Kubernetes kubernetes) : BackgroundService
//   {
//     private readonly CompositeDisposable _disposables = new();
//     private readonly BlockingCollection<ApplicationDefinition> _upQueue = new();
//     private readonly BlockingCollection<ApplicationDefinition> _downQueue = new();
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//       var stacks = await context.Applications.ToArrayAsync(cancellationToken: stoppingToken);
//       var workspace = await LocalWorkspace.CreateAsync(new LocalWorkspaceOptions()
//       {
//         Logger = logger,
//         StackSettings = stacks.ToDictionary(z => z.Name, z => z.Settings),
//         Program = CreateProgram(),
//       }, stoppingToken);
//
//       /*
//        uptimekuma:
//            source: terraform-provider
//            version: 0.12.0
//            parameters:
//              - kill3r-queen/uptimekuma
//        */
//       _ = Task.Run(ProcessBlockingCollection(_upQueue, workspace, ApplicationUp, stoppingToken), stoppingToken);
//       _ = Task.Run(ProcessBlockingCollection(_downQueue, workspace, ApplicationDown, stoppingToken), stoppingToken);
//
//       var applications = GetApplications()
//         .Connect()
//         .OnItemAdded(z => _upQueue.Add(z, stoppingToken))
//         .OnItemRefreshed(z => _upQueue.Add(z, stoppingToken))
//         .OnItemRemoved(z => _downQueue.Add(z, stoppingToken))
//         .Subscribe();
//       _disposables.Add(applications);
//     }
//
//     private Func<Task> ProcessBlockingCollection<T>(BlockingCollection<T> collection, LocalWorkspace workspace, Func<LocalWorkspace, T, CancellationToken , Task> action, CancellationToken cancellationToken) =>
//       async () =>
//       {
//         await Task.Yield();
//         while (collection.TryTake(out var item, Timeout.Infinite))
//         {
//           await action(workspace, item, cancellationToken);
//         }
//       };
//
//     private async Task ApplicationUp(LocalWorkspace workspace, ApplicationDefinition application, CancellationToken cancellationToken)
//     {
//       var key = application.GetApplicationKey();
//       var stack = await WorkspaceStack.CreateOrSelectAsync(key, workspace, cancellationToken);
//       // plugins
//       // config
//       await stack.UpAsync(cancellationToken: cancellationToken);
//     }
//
//     private async Task ApplicationDown(LocalWorkspace workspace, ApplicationDefinition application, CancellationToken cancellationToken)
//     {
//       var key = application.GetApplicationKey();
//       var stack = await WorkspaceStack.CreateOrSelectAsync(key, workspace, cancellationToken);
//       // plugins
//       // config
//       await stack.DestroyAsync(cancellationToken: cancellationToken);
//     }
//
//     private IObservableList<ApplicationDefinition> GetApplications()
//     {
//       var clusterDefinitionClient = new ClusterDefinitionClient(kubernetes);
//       var clusterItems = Observe<ClusterDefinitionList, ClusterDefinition>(clusterDefinitionClient, z => z.Items);
//
//       return clusterItems
//         .Connect()
//         .TransformAsync(async cluster =>
//         {
//           ApplicationDefinitionClient applicationClient;
//           if (cluster.Spec.Secret is null)
//           {
//             applicationClient = new ApplicationDefinitionClient(kubernetes);
//           }
//           else
//           {
//             var externalSecret = await kubernetes.ReadNamespacedSecretAsync(cluster.Spec.Secret, "sgc");
//             var config =
//               await KubernetesClientConfiguration.LoadKubeConfigAsync(
//                 new MemoryStream(externalSecret.Data["kubeconfig.json"]));
//             var remoteCluster = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigObject(config));
//             applicationClient = new ApplicationDefinitionClient(remoteCluster);
//           }
//
//           return (cluster, applicationClient);
//         })
//         .TransformMany(a =>
//         {
//           var (cluster, client) = a;
//           return Observe<ApplicationDefinitionList, ApplicationDefinition>(client, list =>
//             list.Items.Do(application =>
//             {
//               application.Metadata.Annotations ??= new Dictionary<string, string>();
//               application.Metadata.Labels ??= new Dictionary<string, string>();
//               application.Metadata.Labels["driscoll.dev/cluster"] = cluster.Metadata.Name;
//               application.Metadata.Annotations["driscoll.dev/clusterTitle"] = cluster.Spec.Name;
//               application.Metadata.Labels["driscoll.dev/namespace"] = application.Metadata.Namespace();
//             }));
//         })
//         .AsObservableList();
//     }
//
//     private IObservableList<T> Observe<TList, T>(GenericClient client, Func<TList, IEnumerable<T>> getItems)
//       where T : IKubernetesObject<V1ObjectMeta>
//       where TList : IKubernetesList<T>, IKubernetesObject<V1ListMeta>
//     {
//       var comparer = EqualityComparer<T>.Create((arg1, arg2) => arg1 is not null && arg2 is not null &&
//                                                                 string.Equals(
//                                                                   $"{arg1.GetClusterNameAndTitle().ClusterName}-{arg1.Metadata.Name}",
//                                                                   $"{arg2.GetClusterNameAndTitle().ClusterName}-{arg2.Metadata.Name}",
//                                                                   StringComparison.OrdinalIgnoreCase));
//
//
//       var sourceList = new SourceList<T>();
//       var watcher = client.Watch<TList>((type, list) =>
//       {
//         var items = getItems(list);
//         switch (type)
//         {
//           case WatchEventType.Added:
//             sourceList.AddRange(items);
//             break;
//           case WatchEventType.Modified:
//             sourceList.Edit(objects =>
//             {
//               foreach (var item in list.Items)
//               {
//                 var result = objects.SingleOrDefault(z => item.Equals(z));
//                 if (result != null)
//                 {
//                   objects.Replace(result, item);
//                 }
//                 else
//                 {
//                   objects.Add(item);
//                 }
//               }
//             });
//             break;
//           case WatchEventType.Deleted:
//             sourceList.Edit(objects =>
//               objects.RemoveMany(list.Items.Join(objects, z => z, z => z, (a, b) => b, comparer)));
//             break;
//           case WatchEventType.Error:
//             break;
//           case WatchEventType.Bookmark:
//             break;
//           default:
//             throw new ArgumentOutOfRangeException(nameof(type), type, null);
//         }
//       }, exception => { exception.Dump(); }, () => { "closed".Dump(); });
//
//       _disposables.Add(watcher);
//       _disposables.Add(sourceList);
//       return sourceList.AsObservableList();
//     }
//
//     private static PulumiFn CreateProgram()
//     {
//       var config = new Pulumi.Config();
//
//       return PulumiFn.Create(async ct =>
//       {
//         var host = new ProxmoxHost("host", new ()
//         {
//           Cloudflare = ,
//           InternalIpAddress = ,
//           IsBackupServer = ,
//           Proxmox = ,
//           TailscaleIpAddress = ,
//         });
//       });
//     }
//   }
