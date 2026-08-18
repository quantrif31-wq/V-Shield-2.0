using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public sealed class ChatControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public ChatControllerTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int NextId() => System.Threading.Interlocked.Increment(ref _idCounter);

    private static string CreateJwtToken(int userId, string username, string role, int employeeId)
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Role, role),
            new("employeeId", employeeId.ToString()),
            new("token_version", "0"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(
            issuer: "VShieldAPI",
            audience: "VShieldClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (int userId, int me, int other) SeedEmployees()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var meId = 10000 + NextId();
        var otherId = 20000 + NextId();
        var me = new Employee { EmployeeId = meId, FullName = "ChatMe " + suffix, Status = true };
        var other = new Employee { EmployeeId = otherId, FullName = "ChatOther " + suffix, Status = true };
        db.Employees.Add(me);
        db.Employees.Add(other);

        var userId = 30000 + NextId();
        db.AppUsers.Add(new AppUser
        {
            UserId = userId,
            Username = "chat" + suffix,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Chat@12345"),
            FullName = "Chat User " + suffix,
            Role = "Staff",
            IsActive = true,
            TokenVersion = 0,
            EmployeeId = meId,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        return (userId, meId, otherId);
    }

    [Fact]
    public async Task GetContacts_ReturnsActiveEmployees()
    {
        var (userId, me, other) = SeedEmployees();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId, "chatuser", "Staff", me));

        var response = await client.GetAsync("/api/chat/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains(doc.RootElement.GetProperty("data").EnumerateArray(),
            e => e.GetProperty("employeeId").GetInt32() == other);
    }

    [Fact]
    public async Task CreateConversation_ThenSendMessage_ThenList()
    {
        var (userId, me, other) = SeedEmployees();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId, "chatuser", "Staff", me));

        var create = await client.PostAsJsonAsync("/api/chat/conversations", new { employeeIds = new[] { other }, title = "Test" });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var createDoc = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
        var conversationId = createDoc.RootElement.GetProperty("data").GetProperty("conversationId").GetInt32();

        var msg = await client.PostAsJsonAsync($"/api/chat/conversations/{conversationId}/messages",
            new { content = "Hello " + Guid.NewGuid().ToString("N")[..4], messageType = "Text" });
        Assert.Equal(HttpStatusCode.OK, msg.StatusCode);
        var msgDoc = await JsonDocument.ParseAsync(await msg.Content.ReadAsStreamAsync());
        Assert.True(msgDoc.RootElement.GetProperty("success").GetBoolean());

        var list = await client.GetAsync($"/api/chat/conversations/{conversationId}/messages?skip=0&take=50");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listDoc = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
        Assert.True(listDoc.RootElement.GetProperty("data").GetArrayLength() >= 1);

        var convs = await client.GetAsync("/api/chat/conversations");
        Assert.Equal(HttpStatusCode.OK, convs.StatusCode);
        var convsDoc = await JsonDocument.ParseAsync(await convs.Content.ReadAsStreamAsync());
        Assert.True(convsDoc.RootElement.GetProperty("data").GetArrayLength() >= 1);

        var read = await client.PostAsync($"/api/chat/conversations/{conversationId}/read", null);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task CreateConversation_EmptyParticipants_ReturnsBadRequest()
    {
        var (userId, me, _) = SeedEmployees();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId, "chatuser", "Staff", me));

        var response = await client.PostAsJsonAsync("/api/chat/conversations", new { employeeIds = new int[0], title = "X" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ToUnknownConversation_Forbidden()
    {
        var (userId, me, _) = SeedEmployees();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId, "chatuser", "Staff", me));

        var response = await client.PostAsJsonAsync("/api/chat/conversations/999999/messages", new { content = "hi" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_EmptyContent_ReturnsBadRequest()
    {
        var (userId, me, other) = SeedEmployees();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId, "chatuser", "Staff", me));

        var create = await client.PostAsJsonAsync("/api/chat/conversations", new { employeeIds = new[] { other } });
        var createDoc = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
        var conversationId = createDoc.RootElement.GetProperty("data").GetProperty("conversationId").GetInt32();

        var response = await client.PostAsJsonAsync($"/api/chat/conversations/{conversationId}/messages", new { content = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}