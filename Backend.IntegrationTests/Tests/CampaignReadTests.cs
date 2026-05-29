using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Fixtures;

namespace Backend.IntegrationTests.Tests;

public class CampaignReadTests : IClassFixture<KtcWebFactory>
{
    private readonly HttpClient _client;

    public CampaignReadTests(KtcWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllCampaigns_Returns200()
    {
        var response = await _client.GetAsync("/api/campaign");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllCampaigns_ReturnsList()
    {
        var list = await _client.GetFromJsonAsync<List<object>>("/api/campaign");
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetCampaignById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/campaign/999999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
