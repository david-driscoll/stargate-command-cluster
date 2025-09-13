using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace models.Conventions;

public static class KiotaServiceCollectionExtensions
{
  public static IHttpClientBuilder AddKiotaHandlers(this IHttpClientBuilder builder, IServiceCollection services)
  {
    var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
    foreach(var handler in kiotaHandlers)
    {
      services.TryAddTransient(handler.Type);
      builder.AddHttpMessageHandler(sp => (DelegatingHandler)sp.GetRequiredService(handler.Type));
    }

    return builder;
  }
}
