using applications.Models.Authentik;
using Pulumi.Authentik;
using Riok.Mapperly.Abstractions;

namespace authentik.AuthentikResources;

[Mapper]
public static partial class FlowMappings
{

  public static partial void MapProviderArgs([MappingTarget] ProviderProxyArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderProxyArgs args, AuthentikProviderProxy instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderOauth2Args args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderOauth2Args args, AuthentikProviderOauth2 instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSamlArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSamlArgs args, AuthentikProviderSaml instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderLdapArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderLdapArgs args, AuthentikProviderLdap instance);

  public static partial void MapProviderArgs([MappingTarget] SourceSamlArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] SourceSamlArgs args, AuthentikProviderSaml instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRacArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRacArgs args, AuthentikProviderRac instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRadiusArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRadiusArgs args, AuthentikProviderRadius instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSsfArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSsfArgs args, AuthentikProviderSsf instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderScimArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderScimArgs args, AuthentikProviderScim instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderMicrosoftEntraArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderMicrosoftEntraArgs args,
    AuthentikProviderMicrosoftEntra instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderGoogleWorkspaceArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderGoogleWorkspaceArgs args,
    AuthentikProviderGoogleWorkspace instance);
}
