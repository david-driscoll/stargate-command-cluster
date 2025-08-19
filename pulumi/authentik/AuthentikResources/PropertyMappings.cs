using System.Collections.Frozen;
using System.Collections.Generic;
using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class PropertyMappings : SharedComponentResource
{
  private readonly FrozenDictionary<string, ScopeMappingArgs> _oauthScopes = new Dictionary<string, ScopeMappingArgs>()
  {
    ["vikunja_scope"] = new()
    {
      Description = "Enable better vikunja support in authentik (https://vikunja.io/docs/openid/#setup-in-authentik)",
      Expression = """
                   groupsDict = {"vikunja_groups": []}
                   for group in request.user.ak_groups.all():
                     groupsDict["vikunja_groups"].append({"name": group.name, "oidcID": group.num_pk})
                   return groupsDict
                   """
    },
    ["goauthentik.io/api"] = new()
    {
      Name = "OAuth Mapping: authentik API access",
      Description = "authentik API Access on behalf of your user",
      Expression = "return {}"
    },
    ["ak_proxy"] = new()
    {
      Name = "OAuth Mapping: Proxy outpost",
      Description = "authentik Proxy - User information",
      Expression = """
                   # This mapping is used by the authentik proxy. It passes extra user attributes,
                   # which are used for example for the HTTP-Basic Authentication mapping.
                   return {
                       "ak_proxy": {
                           "user_attributes": request.user.group_attributes(request),
                           "is_superuser": request.user.is_superuser,
                       }
                   }
                   """
    },
    ["entitlements"] = new()
    {
      Name = "OAuth Mapping: Application Entitlements",
      Description = "Application entitlements",
      Expression = """
                   entitlements = [entitlement.name for entitlement in request.user.app_entitlements(provider.application)]
                   return {
                       "entitlements": entitlements,
                       "roles": entitlements,
                   }
                   """
    },
    ["email"] = new()
    {
      Name = "OAuth Mapping: OpenID 'email'",
      Description = "Email address",
      Expression = """
                   return {
                       "email": request.user.email,
                       "email_verified": True
                   }
                   """
    },
    ["profile"] = new()
    {
      Name = "OAuth Mapping: OpenID 'profile'",
      Description = "General Profile Information",
      Expression = """
                   return {
                       # Because authentik only saves the user's full name, and has no concept of first and last names,
                       # the full name is used as given name.
                       # You can override this behaviour in custom mappings, i.e. `request.user.name.split(" ")`
                       "name": request.user.name,
                       "given_name": request.user.name,
                       "preferred_username": request.user.username,
                       "nickname": request.user.username,
                       "groups": [group.name for group in request.user.ak_groups.all()],
                   }
                   """
    },
    ["openid"] = new()
    {
      Name = "OAuth Mapping: OpenID 'openid'",
      Description = "",
      Expression = """
                   return {}
                   """
    },
    ["offline_access"] = new()
    {
      Name = "OAuth Mapping: OpenID 'offline_access'",
      Description = "Access to request new tokens without interaction",
      Expression = """
                   return {}
                   """
    },
  }.ToFrozenDictionary();

  private readonly FrozenDictionary<string, ScopeMapping> _scopeMappings;

  public ScopeMapping GetScopeMapping(string scopeName) => !_scopeMappings.TryGetValue(scopeName, out var mapping) ? throw new KeyNotFoundException($"Scope mapping for '{scopeName}' not found.") : mapping;

  public PropertyMappings(ComponentResourceOptions? options = null) : base(
    "custom:resource:AuthentikPropertyMappings",
    "authentik-property-mappings", options)
  {
    _scopeMappings = _oauthScopes.ToFrozenDictionary(
      kvp => kvp.Key,
      kvp =>
      {
        kvp.Value.ScopeName = $"driscoll.dev/{kvp.Key}";
        return new ScopeMapping(kvp.Key, kvp.Value, _parent);
      });
  }
}
