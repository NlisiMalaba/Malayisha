using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Malayisha.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<MalayishaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(MalayishaWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
