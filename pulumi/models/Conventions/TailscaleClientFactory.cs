using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Tailscale.Client;

namespace models.Conventions;

class TailscaleClientFactory(
  HttpClient httpClient,
  TailscaleAccessTokenProvider tokenProvider)
{
  public TailscaleClient GetClient() =>
    new(
      new HttpClientRequestAdapter(
        new BaseBearerTokenAuthenticationProvider(tokenProvider),
        httpClient: httpClient
      )
    );
}
