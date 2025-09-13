using System;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using OnePasswordConnectApi.Client;

namespace models.Conventions;

class OnePasswordConnectClientFactory(HttpClient httpClient)
{
  public OnePasswordConnectClient GetClient() =>
    new(
      new HttpClientRequestAdapter(
        new ApiKeyAuthenticationProvider(
          $"Bearer {Environment.GetEnvironmentVariable("CONNECT_TOKEN") ?? throw new InvalidOperationException("CONNECT_TOKEN is not set")}",
          "Authorization",
          ApiKeyAuthenticationProvider.KeyLocation.Header
        ),
        httpClient: httpClient
      )
    );
}
