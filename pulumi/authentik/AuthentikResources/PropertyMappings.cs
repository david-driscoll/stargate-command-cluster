using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class PropertyMappings : SharedComponentResource
{
  private readonly FrozenDictionary<string, ScopeMappingArgs> _oauthScopes = new Dictionary<string, ScopeMappingArgs>()
  {
    ["immich_role"] = new()
    {
      Description =
        "Enable better Immich support in authentik (https://docs.immich.app/advanced/authentication/authentik/)",
      Expression = """
                   return {"immich_role": "admin" if request.user.is_superuser else "user"}
                   """
    },
    ["vikunja"] = new()
    {
      Description = "Enable better vikunja support in authentik (https://vikunja.io/docs/openid/#setup-in-authentik)",
      Expression = """
                   groupsDict = {"vikunja_groups": []}
                   for group in request.user.ak_groups.all():
                     groupsDict["vikunja_groups"].append({"name": group.name, "oidcID": group.num_pk})
                   return groupsDict
                   """
    },
  }.ToFrozenDictionary();

  private readonly FrozenDictionary<string, ScopeMapping> _scopeMappings;
  private readonly FrozenDictionary<string, Output<GetPropertyMappingProviderScopeResult>> _defaultScopeMappings;

  public Output<string> GetScopeMappingId(string scopeName) => _scopeMappings.TryGetValue(scopeName, out var mapping)
    ? mapping.Id
    : _defaultScopeMappings.TryGetValue(scopeName, out var defaultMapping)
      ? defaultMapping.Apply(z => z.Id)
      : throw new KeyNotFoundException($"Scope mapping for '{scopeName}' not found.");

  public PropertyMappings(ComponentResourceOptions? options = null) : base(
    "custom:resource:AuthentikPropertyMappings",
    "authentik-property-mappings", options)
  {
     Output<GetPropertyMappingProviderScopeResult> GetByScopeName(string scopeName) =>
      GetPropertyMappingProviderScope.Invoke(new () { ScopeName = scopeName }, new InvokeOptions() { Parent = this });

    string[] defaultScopeMappings =
    [
      "goauthentik.io/api",
      "ak_proxy",
      "entitlements",
      "email",
      "profile",
      "openid",
      "offline_access"
    ];
    var defaultScopes = defaultScopeMappings.ToDictionary(z => z, GetByScopeName);
    var customScopes = _oauthScopes.ToFrozenDictionary(
      kvp => kvp.Key,
      kvp =>
      {
        kvp.Value.ScopeName = kvp.Key;
        return new ScopeMapping(kvp.Key, kvp.Value, _parent);
      });

    _scopeMappings = customScopes.ToFrozenDictionary(z => z.Key, z => z.Value);
    _defaultScopeMappings = defaultScopes.ToFrozenDictionary(z => z.Key, z => z.Value);
  }
}
