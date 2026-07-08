using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Seven.Domain.Common;

namespace Seven.Tests.Integration;

/// <summary>
/// 健康检查 API 集成测试
/// </summary>
public class HealthApiTests : IClassFixture<SevenWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthApiTests(SevenWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/Health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WebResponseContent>();
        body.Should().NotBeNull();
        body!.Status.Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheck_Endpoint_ShouldBeHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
