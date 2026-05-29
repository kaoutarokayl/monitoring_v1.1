using System.Net;
using System.Text;
using System.Text.Json;
using Backend.IntegrationTests.Fixtures;

namespace Backend.IntegrationTests.Tests;

public class MiddlewareTests : IClassFixture<KtcWebFactory>
{
    private readonly HttpClient _client;

    public MiddlewareTests(KtcWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCampaign_EmptyName_Returns400()
    {
        var body = JsonSerializer.Serialize(new { name = "" });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/campaign", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_EmptyName_Returns400()
    {
        var body = JsonSerializer.Serialize(new { groupName = "" });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/group", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
