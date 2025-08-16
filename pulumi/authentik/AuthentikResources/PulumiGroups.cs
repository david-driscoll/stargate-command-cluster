using Pulumi;

namespace authentik.AuthentikResources;

public static class PulumiGroups
{
  public static ComponentResource DefaultFlows = new("custom:resource:DefaultFlows", "authentik-default-flows");
  public static ComponentResource DefaultStages = new("custom:resource:DefaultStages", "authentik-default-stages");
}