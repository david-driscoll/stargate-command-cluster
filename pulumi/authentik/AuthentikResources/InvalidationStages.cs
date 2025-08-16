using Pulumi;
using Pulumi.Authentik;

namespace applications.AuthentikResources;

public class InvalidationStages(ComponentResourceOptions options) : SharedComponentResource("custom:resource:InvalidationStages", "stages-invalidation", options)
{
  public StageUserLogout Logout => field ??= new("custom-authentication-logout", new(), _parent);

  public StageUserLogout ProviderLogout =>
    field ??= new("custom-authentication-provider-logout", new(), _parent);
}