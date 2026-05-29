using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Fixtures;

namespace Backend.IntegrationTests.Tests;

public class GroupReadTests : IClassFixture<KtcWebFactory>
{
    private readonly HttpClient _client;

    public GroupReadTests(KtcWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllGroups_Returns200()
    {
        var response = await _client.GetAsync("/api/group");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllGroups_ReturnsList()
    {
        var list = await _client.GetFromJsonAsync<List<object>>("/api/group");
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetGroupById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/group/999999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
