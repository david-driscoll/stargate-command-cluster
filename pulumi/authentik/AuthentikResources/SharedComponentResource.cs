using Pulumi;

namespace authentik.AuthentikResources;

public abstract class SharedComponentResource : ComponentResource
{
  protected CustomResourceOptions _parent;

  public SharedComponentResource(string type, string name, ComponentResourceOptions? options = null) : base(type, name,
    options)
  {
    _parent = new () { Parent = this };
  }
}
