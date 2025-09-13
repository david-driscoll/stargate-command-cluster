using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dumpify;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Pulumi;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Tailscale.Client;

public static class TailscaleUpdater
{
  public static async Task DoUpdate(GetAPICredentialArgs credentialArgs, Rocket.Surgery.OnePasswordNativeUnofficial.Provider provider)
  {
    var credential = await GetAPICredential.InvokeAsync(credentialArgs, new InvokeOptions() { Provider = provider });
    var client = new HttpClient(new HttpClientHandler()
    {
      ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
    var tokenRequest = new FormUrlEncodedContent([
      new ("client_id", credential.Username),
      new ("client_secret", credential.Credential)
    ]);

    var tokenResponse = await client.PostAsync("https://api.tailscale.com/api/v2/oauth/token", tokenRequest);
    tokenResponse.EnsureSuccessStatusCode();

    var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
    // Parse the JSON response to get the access token
    var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tokenContent);
    var accessToken = tokenData["access_token"].ToString();

    var authProvider = new ApiKeyAuthenticationProvider($"Bearer {accessToken}", "Authorization", ApiKeyAuthenticationProvider.KeyLocation.Header);
    var adapter = new HttpClientRequestAdapter(authProvider, httpClient: client) { BaseUrl = "https://api.tailscale.com/api/v2/" };
    var tsClient = new TailscaleClient(adapter);
    (await tsClient.Tailnet["-"].Users.GetAsUsersGetResponseAsync()).Dump();
  }

}