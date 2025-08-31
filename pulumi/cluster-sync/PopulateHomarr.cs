using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dumpify;
using k8s;
using models.Applications;
using Rocket.Surgery.OnePasswordNativeUnofficial;

namespace applications;

public static class PopulateHomarr
{
  public static async Task Populate(Kubernetes localCluster, GetAPICredentialResult credential)
  {
    var client = new HomarrClient(new System.Net.Http.HttpClient(new HttpClientHandler()
    {
      ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
    })
    {
      BaseAddress = new Uri($"https://{credential.Hostname}/"),
      DefaultRequestHeaders =
      {
        { "ApiKey", credential.Credential }
      },
    });

    var applications =
      (await localCluster.ListClusterCustomObjectAsync<ApplicationDefinitionList>("driscoll.dev", "v1",
        "applicationdefinitions")).Items.Where(z => !string.IsNullOrWhiteSpace(z.Spec.Url)).ToImmutableArray();

    var existingHomarrApplications = await client.GetApplications();

    DumpNames("existingHomarrApplications", existingHomarrApplications.Select(z => z.Href));

    var missingRemoteEntities =
      applications.ExceptBy(existingHomarrApplications.Select(z => z.Href), definition => definition.Spec.Url);
    var removedRemoteEntities =
      existingHomarrApplications.ExceptBy(applications.Select(z => z.Spec.Url), application => application.Href);

    DumpNames("missingRemoteEntities", missingRemoteEntities.Select(z => z.Spec.Url!));
    DumpNames("removedRemoteEntities", removedRemoteEntities.Select(z => z.Href));

    foreach (var removedEntity in removedRemoteEntities)
    {
      await client.DeleteApplication(removedEntity.Id);
    }

    foreach (var missingEntity in missingRemoteEntities)
    {
      await client.CreateApplication(new UpdateHomarrApplication(
        missingEntity.Spec.Name,
        missingEntity.Spec.Description,
        missingEntity.Spec.Icon,
        missingEntity.Spec.Url,
        missingEntity.Spec.Uptime?.Http?.Url ?? missingEntity.Spec.Url
      ));
    }

    foreach (var (application, definition) in existingHomarrApplications.Join(applications, z => z.Href,
               z => z.Spec.Url, (app, def) => (app, def)))
    {
      await client.UpdateApplication(application.Id, new UpdateHomarrApplication(
        definition.Spec.Name,
        definition.Spec.Description,
        definition.Spec.Icon,
        definition.Spec.Url,
        definition.Spec.Uptime?.Http?.Url ?? definition.Spec.Url
      ));
    }
  }

  static void DumpNames(string title, IEnumerable<string> resources)
  {
    resources.Distinct().Dump(title);
  }
}
