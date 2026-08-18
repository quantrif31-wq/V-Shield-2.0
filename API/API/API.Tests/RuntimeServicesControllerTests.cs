using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public sealed class RuntimeServicesControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly SecurityWebApplicationFactory _factory;

    public RuntimeServicesControllerTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static string AdminToken()
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1002"),
            new Claim(JwtRegisteredClaimNames.UniqueName, "admin.test"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("token_version", "0"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken("VShieldAPI", "VShieldClient", claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetAll_ReturnsServices()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.GetAsync("/api/runtime-services");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetArrayLength() >= 4);
    }

    [Fact]
    public async Task UpdateConfig_KnownService_UpdatesFlags()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.PutAsJsonAsync("/api/runtime-services/python_qr", new { enabled = false, autoStart = false });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("python_qr", doc.RootElement.GetProperty("name").GetString());
        Assert.False(doc.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task UpdateConfig_UnknownService_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.PutAsJsonAsync("/api/runtime-services/nope", new { enabled = true });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}