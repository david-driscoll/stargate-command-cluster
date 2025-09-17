using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Pulumi;
using Rocket.Surgery.DependencyInjection;
using Rocket.Surgery.OnePasswordNativeUnofficial;

namespace models.Conventions;

[ServiceRegistration(ServiceLifetime.Singleton)]
internal class TailscaleAccessTokenProvider : IAccessTokenProvider
{
  public async Task<string> GetAuthorizationTokenAsync(Uri uri,
    Dictionary<string, object>? additionalAuthenticationContext = null,
    CancellationToken cancellationToken = new CancellationToken())
  {
    var credentials = await GetAPICredential.InvokeAsync(new (){ Title = "Tailscale Terraform OAuth Client", Id = "kfpxs5w6zr3qetocx3wmdk3wxy", Vault = "Eris"});
    using var httpClient = new HttpClient(new HttpClientHandler()
    {
      // ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
    var tokenRequest = new FormUrlEncodedContent([
      new("client_id", credentials.Username),
      new("client_secret", credentials.Credential)
    ]);
    var tokenResponse = await httpClient.PostAsync("oauth/token", tokenRequest, cancellationToken);
    tokenResponse.EnsureSuccessStatusCode();

    var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
    var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tokenContent);
    return tokenData!["access_token"].ToString()!;
  }

  public AllowedHostsValidator AllowedHostsValidator { get; set; } = new();
}
