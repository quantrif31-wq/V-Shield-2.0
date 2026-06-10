using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Middleware;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public class SecurityBoundaryTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityBoundaryTests(SecurityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health/degraded")]
    [InlineData("/api/pre-registrations/validate/unknown-token")]
    [InlineData("/api/pre-registrations/visitor-pass/unknown-token")]
    public async Task PublicGetEndpoints_DoNotRequireAuthentication(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/employees")]
    [InlineData("/api/access-permissions")]
    [InlineData("/api/camera-runtime")]
    [InlineData("/api/vehicles")]
    [InlineData("/api/QrAccess/verify-camera-auth")]
    [InlineData("/api/dynamic-qr/generate")]
    [InlineData("/api/video/1/content")]
    public async Task PrivilegedEndpoints_RejectAnonymousRequests(string path)
    {
        using var request = path.Contains("verify-camera-auth", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("dynamic-qr/generate", StringComparison.OrdinalIgnoreCase)
            ? new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new { })
            }
            : new HttpRequestMessage(HttpMethod.Get, path);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Uploads_AreNotPublic()
    {
        var response = await _client.GetAsync("/uploads/faces/anything.jpg");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_IsReturnedAndAccepted()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-123");

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values));
        Assert.Equal("test-correlation-123", values.Single());
    }

    [Fact]
    public async Task ReadinessHealth_ReturnsDependencyChecks()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ready\"", body);
        Assert.Contains("\"database\"", body);
    }

    [Fact]
    public async Task DegradedHealth_ReturnsRuntimeChecks()
    {
        var response = await _client.GetAsync("/health/degraded");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"runtime\"", body);
        Assert.Contains("\"database\"", body);
    }

    [Fact]
    public async Task SafeExceptionEnvelope_DoesNotLeakExceptionDetails()
    {
        var response = await _client.GetAsync("/__test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("correlationId", body);
        Assert.DoesNotContain("Sensitive test exception detail", body);
    }

    [Fact]
    public async Task StaffLogin_IssuesRefreshToken_AndLogoutRevokesAccessToken()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "staff.test",
            password = "Staff@12345"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await ReadJsonAsync(loginResponse);
        var token = GetString(login, "token");
        var refreshToken = GetString(login, "refreshToken");
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        using var authenticated = CreateClientWithBearer(token);
        var meResponse = await authenticated.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var logoutResponse = await authenticated.PostAsJsonAsync("/api/Auth/logout", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var revokedMeResponse = await authenticated.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedMeResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RotatesRefreshToken()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "staff.test",
            password = "Staff@12345"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await ReadJsonAsync(loginResponse);
        var refreshToken = GetString(login, "refreshToken");

        var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refresh = await ReadJsonAsync(refreshResponse);
        Assert.NotEqual(refreshToken, GetString(refresh, "refreshToken"));
        Assert.False(string.IsNullOrWhiteSpace(GetString(refresh, "token")));
    }

    [Fact]
    public async Task StaffToken_CannotAccessAdminOnlyUsersController()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminLogin_RequiresTotpSetup_ThenAcceptsTotp()
    {
        var setupResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "admin.test",
            password = "Admin@12345"
        });

        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        var setup = await ReadJsonAsync(setupResponse);
        Assert.True(setup.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.True(setup.RootElement.GetProperty("requiresMfaSetup").GetBoolean());
        var secret = GetString(setup, "mfaSetupSecret");
        Assert.False(string.IsNullOrWhiteSpace(secret));

        var code = GenerateTotpCode(secret);
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "admin.test",
            password = "Admin@12345",
            mfaCode = code
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await ReadJsonAsync(loginResponse);
        Assert.False(login.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(GetString(login, "token")));
    }

    private HttpClient CreateClientWithBearer(string token)
    {
        var client = _client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static string GetString(JsonDocument document, string propertyName) =>
        document.RootElement.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static string GenerateTotpCode(string secret)
    {
        var secretBytes = Base32Decode(secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6");
    }

    private static string CreateJwtToken(int userId, string username, string role)
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("token_version", "0"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: "VShieldAPI",
            audience: "VShieldClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sanitized = input.Trim().Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in sanitized)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
                throw new FormatException("Invalid base32 value.");

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 255));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }
}
