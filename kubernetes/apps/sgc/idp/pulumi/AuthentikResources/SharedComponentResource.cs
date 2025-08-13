using Pulumi;

namespace applications.AuthentikResources;

public abstract class SharedComponentResource : ComponentResource
{
  protected readonly CustomResourceOptions _parent;
  protected readonly ComponentResourceOptions _cparent;

  public SharedComponentResource(string type, string name, ComponentResourceOptions? options = null) : base(type, name,
    options)
  {
    _parent = new () { Parent = this };
  }
}
