using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Seven.Domain.Common;

namespace Seven.Tests.Integration;

/// <summary>
/// 认证 API 集成测试
/// </summary>
public class AuthApiTests : IClassFixture<SevenWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(SevenWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            userName = "admin",
            password = "123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WebResponseContent>();
        body.Should().NotBeNull();
        body!.Status.Should().BeTrue();
        body.Message.Should().Be("登录成功");

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(body.Data));
        doc.RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldFail()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            userName = "admin",
            password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WebResponseContent>();
        body!.Status.Should().BeFalse();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/Sys_User/getPageData", new { page = 1, rows = 10 });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithToken_ShouldReturnData()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            userName = "admin",
            password = "123456"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<WebResponseContent>();
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(loginBody!.Data));
        var token = doc.RootElement.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/Sys_User/getPageData", new { page = 1, rows = 10 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WebResponseContent>();
        body!.Status.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewTokens()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            userName = "admin",
            password = "123456"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<WebResponseContent>();
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(loginBody!.Data));
        var refreshToken = doc.RootElement.GetProperty("refreshToken").GetString();

        var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<WebResponseContent>();
        refreshBody!.Status.Should().BeTrue();
    }
}
