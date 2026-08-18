using API.Data;
using API.Hubs;
using API.Models;
using API.Services.ChatRelay;
using API.Services.Sync;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Security.Claims;
using Xunit;

namespace API.Tests;

public sealed class HubTestHelpers
{
    public static void SetHubMembers(Hub hub, HubCallerContext? context = null, IHubCallerClients? clients = null, IGroupManager? groups = null)
    {
        SetProperty(hub, nameof(Hub.Context), context);
        SetProperty(hub, nameof(Hub.Clients), clients);
        SetProperty(hub, nameof(Hub.Groups), groups);
    }

    private static void SetProperty(object obj, string name, object? value)
    {
        var setter = typeof(Hub).GetProperty(name)!.GetSetMethod(nonPublic: true)
                     ?? typeof(Hub).GetProperty(name)!.GetSetMethod()!;
        setter.Invoke(obj, new[] { value });
    }

    public static TestHubContext ContextWithClaims(string connectionId, params Claim[] claims) =>
        new(connectionId, claims.Length == 0 ? null : new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));

    public static ClaimsPrincipal ClaimsWithEmployeeId(int employeeId) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim("employeeId", employeeId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, (employeeId + 1000).ToString()),
            new Claim(ClaimTypes.Name, "test.user")
        }, "test"));

    public static ApplicationDbContext CreateInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"{name}_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    public static Mock<IHubCallerClients> CreateCallerClients(out Mock<ISingleClientProxy> caller)
    {
        var clients = new Mock<IHubCallerClients>();
        caller = new Mock<ISingleClientProxy>();
        clients.Setup(c => c.Caller).Returns(caller.Object);
        return clients;
    }
}

public class TestHubContext(string connectionId, ClaimsPrincipal? user) : HubCallerContext
{
    public bool Aborted { get; private set; }
    public override string ConnectionId => connectionId;
    public override string? UserIdentifier => user?.Identity?.Name;
    public override ClaimsPrincipal? User => user;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features => new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;
    public override void Abort() => Aborted = true;
}

public sealed class NotificationHubTests
{
    private static (Mock<IGroupManager> Groups, Mock<ISingleClientProxy> Caller) Setup(
        NotificationHub hub, TestHubContext context)
    {
        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = HubTestHelpers.CreateCallerClients(out var caller);
        HubTestHelpers.SetHubMembers(hub, context, clients.Object, groups.Object);
        return (groups, caller);
    }

    [Fact]
    public async Task OnConnectedAsync_JoinsUserGroup()
    {
        var hub = new NotificationHub();
        var context = (TestHubContext)HubTestHelpers.ContextWithClaims("conn-1",
            new Claim(ClaimTypes.NameIdentifier, "42"));
        var (groups, _) = Setup(hub, context);

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync("conn-1", "notif_user_42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_NoUserId_DoesNotJoin()
    {
        var hub = new NotificationHub();
        var context = (TestHubContext)HubTestHelpers.ContextWithClaims("conn-1");
        var (groups, _) = Setup(hub, context);

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_LeavesUserGroup()
    {
        var hub = new NotificationHub();
        var context = (TestHubContext)HubTestHelpers.ContextWithClaims("conn-1",
            new Claim(ClaimTypes.NameIdentifier, "42"));
        var (groups, _) = Setup(hub, context);

        await hub.OnDisconnectedAsync(null);

        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", "notif_user_42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDelivery_SendsToCaller()
    {
        var hub = new NotificationHub();
        var context = (TestHubContext)HubTestHelpers.ContextWithClaims("conn-1");
        var (_, caller) = Setup(hub, context);

        await hub.ConfirmDelivery(7);

        caller.Verify(c => c.SendCoreAsync("DeliveryConfirmed", It.IsAny<object[]?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class EmployeeStatsHubTests
{
    [Fact]
    public async Task JoinStatsGroup_AddsToStatsGroup()
    {
        var hub = new EmployeeStatsHub();
        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        HubTestHelpers.SetHubMembers(hub,
            (TestHubContext)HubTestHelpers.ContextWithClaims("conn-1"),
            new Mock<IHubCallerClients>().Object, groups.Object);

        await hub.JoinStatsGroup();

        groups.Verify(g => g.AddToGroupAsync("conn-1", "stats", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveStatsGroup_RemovesFromStatsGroup()
    {
        var hub = new EmployeeStatsHub();
        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        HubTestHelpers.SetHubMembers(hub,
            (TestHubContext)HubTestHelpers.ContextWithClaims("conn-1"),
            new Mock<IHubCallerClients>().Object, groups.Object);

        await hub.LeaveStatsGroup();

        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", "stats", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class ChatHubTests
{
    private readonly ChatPresenceRegistry _presence = new();
    private readonly ChatRelayGateway _relay;

    private Mock<IClientProxy> _chatRelayGroupProxy;

    public ChatHubTests()
    {
        var chatClients = new Mock<IHubClients>();
        _chatRelayGroupProxy = new Mock<IClientProxy>();
        chatClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_chatRelayGroupProxy.Object);
        var chatContext = new Mock<IHubContext<ChatHub>>();
        chatContext.Setup(c => c.Clients).Returns(chatClients.Object);

        var relayClients = new Mock<IHubClients>();
        relayClients.Setup(c => c.Client(It.IsAny<string>())).Returns(new Mock<ISingleClientProxy>().Object);
        var relayContext = new Mock<IHubContext<ChatRelayHub>>();
        relayContext.Setup(c => c.Clients).Returns(relayClients.Object);

        var options = Options.Create(new SyncRuntimeOptions { Mode = SyncRuntimeModes.Standalone });
        _relay = new ChatRelayGateway(options, _presence, new ChatRelayNodeRegistry(), chatContext.Object, relayContext.Object, null);
    }

    private ApplicationDbContext SeedDb(ApplicationDbContext db, int empId)
    {
        db.Employees.Add(new Employee
        {
            EmployeeId = empId,
            FullName = "Nguyen Van A",
            Email = $"user{empId}@example.com",
            Status = true
        });
        db.SaveChanges();
        return db;
    }

    private ChatHub CreateHub(ApplicationDbContext db, ClaimsPrincipal? claims,
        out Mock<IGroupManager> groups, out Mock<IClientProxy> groupProxy)
    {
        var hub = new ChatHub(db, NullLogger<ChatHub>.Instance, _presence, _relay);

        groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubCallerClients>();
        groupProxy = new Mock<IClientProxy>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxy.Object);
        clients.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(groupProxy.Object);
        clients.Setup(c => c.Caller).Returns(new Mock<ISingleClientProxy>().Object);

        HubTestHelpers.SetHubMembers(hub,
            new TestHubContext("conn-chat", claims),
            clients.Object, groups.Object);
        return hub;
    }

    private ChatHub CreateHubWithEmployee(ApplicationDbContext db, int empId, out Mock<IGroupManager> groups, out Mock<IClientProxy> groupProxy)
    {
        SeedDb(db, empId);
        return CreateHub(db, HubTestHelpers.ClaimsWithEmployeeId(empId), out groups, out groupProxy);
    }

    [Fact]
    public async Task OnConnectedAsync_JoinsParticipantGroups()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_conn");
        SeedDb(db, 5);
        db.ChatConversations.Add(new ChatConversation { ConversationId = 1 });
        db.ChatParticipants.Add(new ChatParticipant { ConversationId = 1, EmployeeId = 5 });
        db.SaveChanges();

        var hub = CreateHub(db, HubTestHelpers.ClaimsWithEmployeeId(5), out var groups, out _);

        await hub.OnConnectedAsync();

        Assert.True(_presence.IsOnline(5));
        groups.Verify(g => g.AddToGroupAsync("conn-chat", "conv_1", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.AddToGroupAsync("conn-chat", "user_5", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesPresence()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_dis");
        SeedDb(db, 5);
        _presence.Add(5);
        var hub = CreateHub(db, HubTestHelpers.ClaimsWithEmployeeId(5), out _, out _);

        await hub.OnDisconnectedAsync(null);

        Assert.False(_presence.IsOnline(5));
    }

    [Fact]
    public async Task SendMessage_NonParticipant_ReturnsNull()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_np");
        var hub = CreateHubWithEmployee(db, 5, out _, out _);
        db.ChatConversations.Add(new ChatConversation { ConversationId = 1 });
        db.ChatParticipants.Add(new ChatParticipant { ConversationId = 1, EmployeeId = 9 });
        db.SaveChanges();

        var result = await hub.SendMessage(1, "hello");

        Assert.Null(result);
    }

    [Fact]
    public async Task SendMessage_Participant_PersistsAndBroadcasts()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_send");
        var hub = CreateHubWithEmployee(db, 5, out _, out var groupProxy);
        db.ChatConversations.Add(new ChatConversation { ConversationId = 1 });
        db.ChatParticipants.Add(new ChatParticipant { ConversationId = 1, EmployeeId = 5 });
        db.SaveChanges();

        var result = await hub.SendMessage(1, "xin chào");

        Assert.NotNull(result);
        var stored = db.ChatMessages.Single();
        Assert.Equal("xin chào", stored.Content);
        Assert.Equal(1, stored.ConversationId);
        Assert.Equal(5, stored.SenderId);
        groupProxy.Verify(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object[]?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_DuplicateClientMessageId_ReturnsExisting()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_dup");
        var hub = CreateHubWithEmployee(db, 5, out _, out _);
        db.ChatConversations.Add(new ChatConversation { ConversationId = 1 });
        db.ChatParticipants.Add(new ChatParticipant { ConversationId = 1, EmployeeId = 5 });
        db.ChatMessages.Add(new ChatMessage
        {
            ConversationId = 1,
            SenderId = 5,
            Content = "đã gửi",
            ClientMessageId = "abc-1",
            SentAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var result = await hub.SendMessage(1, "gg", "Text", null, "abc-1");

        Assert.NotNull(result);
        Assert.Single(db.ChatMessages);
    }

    [Fact]
    public async Task MarkRead_MarksUnreadMessages()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_read");
        var hub = CreateHubWithEmployee(db, 5, out _, out _);
        SeedDb(db, 6);
        db.ChatConversations.Add(new ChatConversation { ConversationId = 1 });
        db.ChatParticipants.Add(new ChatParticipant { ConversationId = 1, EmployeeId = 5 });
        db.ChatMessages.Add(new ChatMessage { ConversationId = 1, SenderId = 6, Content = "chưa đọc" });
        db.SaveChanges();

        await hub.MarkRead(1);

        Assert.True(db.ChatMessages.Single().IsRead);
        Assert.NotNull(db.ChatMessages.Single().ReadAt);
    }

    [Fact]
    public async Task Typing_BroadcastsToOthers()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_typing");
        var hub = CreateHubWithEmployee(db, 5, out _, out var groupProxy);
        db.ChatConversations.Add(new ChatConversation { ConversationId = 1 });

        await hub.Typing(1);

        groupProxy.Verify(p => p.SendCoreAsync("UserTyping", It.IsAny<object[]?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CallUser_TargetOfflineAndRelayDisabled_NoOp()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_calloff");
        var hub = CreateHubWithEmployee(db, 5, out _, out _);

        await hub.CallUser(9, "offer", "sdp-data");

        Assert.False(_presence.IsOnline(9));
    }

    [Fact]
    public async Task CallUser_TargetOnline_BroadcastsToGateway()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_callonline");
        var hub = CreateHubWithEmployee(db, 5, out _, out _);
        _presence.Add(9);

        await hub.CallUser(9, "offer", "sdp-data");

        Assert.True(_presence.IsOnline(9));
    }

    [Fact]
    public async Task CallResponse_TargetOnline_SendsToTargetGroup()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_resp");
        var hub = CreateHubWithEmployee(db, 5, out _, out var groupProxy);
        _presence.Add(9);

        await hub.CallResponse(9, "answer", "sdp-answer");

        groupProxy.Verify(p => p.SendCoreAsync("CallResponse", It.IsAny<object[]?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndCall_TargetOnline_SendsCallEnded()
    {
        var db = HubTestHelpers.CreateInMemoryDb("chathub_end");
        var hub = CreateHubWithEmployee(db, 5, out _, out var groupProxy);
        _presence.Add(9);

        await hub.EndCall(9, 1);

        groupProxy.Verify(p => p.SendCoreAsync("CallEnded", It.IsAny<object[]?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class ChatRelayHubTests
{
    private static ChatRelayHub CreateHub(ChatRelayGateway gateway)
    {
        var options = Options.Create(new SyncRuntimeOptions());
        var hub = new ChatRelayHub(gateway, new ChatRelayNodeRegistry(), options, NullLogger<ChatRelayHub>.Instance);
        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        HubTestHelpers.SetHubMembers(hub,
            new TestHubContext("node-conn", null),
            new Mock<IHubCallerClients>().Object, groups.Object);
        return hub;
    }

    private static ChatRelayGateway CreateGateway()
    {
        var chatClients = new Mock<IHubClients>();
        chatClients.Setup(c => c.Group(It.IsAny<string>())).Returns(new Mock<IClientProxy>().Object);
        var chatContext = new Mock<IHubContext<ChatHub>>();
        chatContext.Setup(c => c.Clients).Returns(chatClients.Object);
        var relayClients = new Mock<IHubClients>();
        relayClients.Setup(c => c.Client(It.IsAny<string>())).Returns(new Mock<ISingleClientProxy>().Object);
        var relayContext = new Mock<IHubContext<ChatRelayHub>>();
        relayContext.Setup(c => c.Clients).Returns(relayClients.Object);
        return new ChatRelayGateway(
            Options.Create(new SyncRuntimeOptions { Mode = SyncRuntimeModes.AreaNode }),
            new ChatPresenceRegistry(), new ChatRelayNodeRegistry(), chatContext.Object, relayContext.Object, null);
    }

    [Fact]
    public async Task RegisterPresence_ReplacesNodeEmployees()
    {
        var hub = CreateHub(CreateGateway());
        await hub.RegisterPresence([1, 2, 3]);
    }

    [Fact]
    public async Task RegisterPresence_Null_Handled()
    {
        var hub = CreateHub(CreateGateway());
        await hub.RegisterPresence(null!);
        await hub.RegisterPresence([]);
    }

    [Fact]
    public async Task OnConnectedAsync_RejectsWhenNotCentralOrNoNodeCredentials()
    {
        var hub = new ChatRelayHub(CreateGateway(), new ChatRelayNodeRegistry(),
            Options.Create(new SyncRuntimeOptions { Mode = SyncRuntimeModes.AreaNode }), NullLogger<ChatRelayHub>.Instance);
        var context = new TestHubContext("node-conn", null);
        HubTestHelpers.SetHubMembers(hub, context, new Mock<IHubCallerClients>().Object, new Mock<IGroupManager>().Object);

        await hub.OnConnectedAsync();

        Assert.True(context.Aborted);
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesConnection()
    {
        var nodeRegistry = new ChatRelayNodeRegistry();
        nodeRegistry.ReplaceEmployees("node-conn", [1, 2]);
        var hub = new ChatRelayHub(CreateGateway(), nodeRegistry,
            Options.Create(new SyncRuntimeOptions()), NullLogger<ChatRelayHub>.Instance);
        HubTestHelpers.SetHubMembers(hub, new TestHubContext("node-conn", null),
            new Mock<IHubCallerClients>().Object, new Mock<IGroupManager>().Object);

        await hub.OnDisconnectedAsync(null);

        Assert.False(nodeRegistry.TryGetNodeConnection(1, out _));
        Assert.False(nodeRegistry.TryGetNodeConnection(2, out _));
    }
}
