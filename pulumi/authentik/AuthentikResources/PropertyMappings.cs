using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class PropertyMappings : SharedComponentResource
{
  public PropertyMappings(ComponentResourceOptions? options = null) : base(
    "custom:resource:AuthentikPropertyMappings",
    "authentik-property-mappings", options)
  {
    _ = new ScopeMapping("vikunja_scope", new()
    {
      ScopeName = "vikunja_scope",
      Description = "Enable better vikunja support in authentik (https://vikunja.io/docs/openid/#setup-in-authentik)",
      Expression = """
                   groupsDict = {"vikunja_groups": []}
                   for group in request.user.ak_groups.all():
                     groupsDict["vikunja_groups"].append({"name": group.name, "oidcID": group.num_pk})
                   return groupsDict
                   """
    }, _parent);
  }
}
