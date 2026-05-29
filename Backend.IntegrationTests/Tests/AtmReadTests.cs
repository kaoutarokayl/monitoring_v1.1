using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Fixtures;

namespace Backend.IntegrationTests.Tests;

public class AtmReadTests : IClassFixture<KtcWebFactory>
{
    private readonly HttpClient _client;

    public AtmReadTests(KtcWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllClients_Returns200()
    {
        var response = await _client.GetAsync("/api/atm/clients");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllClients_ReturnsNonEmptyList()
    {
        var list = await _client.GetFromJsonAsync<List<object>>("/api/atm/clients");
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetClientById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/atm/clients/999999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHardwareTypes_Returns200()
    {
        var response = await _client.GetAsync("/api/atm/hardwaretypes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllRegions_Returns200()
    {
        var response = await _client.GetAsync("/api/atm/regions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBusinesses_Returns200()
    {
        var response = await _client.GetAsync("/api/atm/businesses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRemoteCommandTypes_Returns200()
    {
        var response = await _client.GetAsync("/api/atm/command-types");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactionTypeLookups_Returns200()
    {
        var response = await _client.GetAsync("/api/atm/transactions/lookups/type-codes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
