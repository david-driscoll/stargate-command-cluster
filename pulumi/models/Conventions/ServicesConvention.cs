using System;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pulumi;
using Rocket.Surgery.Conventions;
using Rocket.Surgery.Conventions.DependencyInjection;

namespace models.Conventions;

[ExportConvention]
public class ServicesConvention : IServiceConvention
{
  public void Register(IConventionContext context, IConfiguration configuration, IServiceCollection services)
  {
    services
      .AddHttpClient<TailscaleClientFactory>((sp, client) => client.BaseAddress = new Uri(sp.GetRequiredService<Config>().Require("tailscaleUrl")))
      .AddKiotaHandlers(services);
    services
      .AddHttpClient<OnePasswordConnectClientFactory>((_, client) => client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("CONNECT_HOST") ?? throw new InvalidOperationException("CONNECT_HOST is not set")))
      .AddKiotaHandlers(services);

    services.AddSingleton(sp => KubernetesClientConfiguration.BuildConfigFromConfigFile(sp.GetRequiredService<Config>().Require("kubeconfig")));
    services.AddSingleton(sp => new Kubernetes(sp.GetRequiredService<KubernetesClientConfiguration>()));
  }
}
