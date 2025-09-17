using System.Collections.Generic;
using k8s;
using models;
using Pulumi;

KubernetesJson.AddJsonOptions(options => { options.Converters.Add(new YamlMemberConverterFactory()); });

return await Deployment.RunAsync(async () =>
{

  // Export outputs here
  return new Dictionary<string, object?>();
});
