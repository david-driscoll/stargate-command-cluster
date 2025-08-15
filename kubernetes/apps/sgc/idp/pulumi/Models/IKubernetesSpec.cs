namespace applications.Models;

public interface IKubernetesSpec
{
  object Spec { get; }
}