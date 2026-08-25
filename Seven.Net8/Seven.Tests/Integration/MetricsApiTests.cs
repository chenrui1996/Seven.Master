using System.Net;
using FluentAssertions;

namespace Seven.Tests.Integration;

public class MetricsApiTests : IClassFixture<SevenWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MetricsApiTests(SevenWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Metrics_ShouldReturnPrometheusText()
    {
        var response = await _client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Match(b =>
            b.Contains("process_") || b.Contains("http_") || b.Contains("dotnet_"));
    }
}
