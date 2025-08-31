using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace applications;

public class HomarrClient(HttpClient client)
{
  public Task<HomarrApplication[]> GetApplications()
  {
    return client.GetFromJsonAsync<HomarrApplication[]>("/api/apps")!;
  }

  public Task<HomarrApplication> GetApplication(string id)
  {
    return client.GetFromJsonAsync<HomarrApplication>($"/api/apps/{id}")!;
  }

  public async Task<CreateResponse> CreateApplication(UpdateHomarrApplication application)
  {
    var response = await client.PostAsJsonAsync("/api/apps", application);
      return await response.Content.ReadFromJsonAsync<CreateResponse>()!;
  }

  public async Task<string> UpdateApplication(string id, UpdateHomarrApplication application)
  {
      var response = await client.PutAsJsonAsync($"/api/apps/{id}", application);
      return await response.Content.ReadAsStringAsync();
  }

  public Task DeleteApplication(string id)
  {
    return client.DeleteAsync($"/api/apps/{id}");
  }
}
