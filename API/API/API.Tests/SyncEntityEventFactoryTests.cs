using System.Text.Json;
using API.Data;
using API.Models;
using API.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.Tests;

public sealed class SyncEntityEventFactoryTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"sef_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static SyncEntityEventFactory Factory(string mode)
    {
        var opts = new SyncRuntimeOptions
        {
            Mode = mode,
            CompanyId = 11,
            SiteId = 22,
            LocalAreaNodeId = "node-7"
        };
        return new SyncEntityEventFactory(Options.Create(opts));
    }

    [Fact]
    public void IsEnabledForCurrentMode_Standalone_False()
    {
        Assert.False(Factory(SyncRuntimeModes.Standalone).IsEnabledForCurrentMode);
        Assert.True(Factory(SyncRuntimeModes.AreaNode).IsEnabledForCurrentMode);
        Assert.True(Factory(SyncRuntimeModes.Central).IsEnabledForCurrentMode);
    }

    [Fact]
    public void BuildCandidates_Standalone_Empty()
    {
        var db = CreateDb();
        db.Gates.Add(new Gate { GateName = "A" });
        var result = Factory(SyncRuntimeModes.Standalone).BuildCandidates(db.ChangeTracker);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildCandidates_AreaNode_OnlyAreaNodeEntities_Published()
    {
        var db = CreateDb();
        db.AccessLogs.Add(new AccessLog());
        db.Employees.Add(new Employee());
        var result = Factory(SyncRuntimeModes.AreaNode).BuildCandidates(db.ChangeTracker);

        var candidate = Assert.Single(result);
        var ev = candidate.OutboxEvent;
        Assert.Equal(nameof(AccessLog), ev.AggregateType);
        Assert.Equal("Sync.AccessLog.Upsert", ev.EventType);
        Assert.Equal("Sync", ev.Channel);
        Assert.Equal("PendingSync", ev.Status);
        Assert.Equal("AreaNode", ev.SourceSystem);
        Assert.False(ev.IsCanonical);
        Assert.NotNull(ev.NextAttemptAtUtc);
        Assert.Equal("node-7", ev.AreaNodeId);
        Assert.Equal(11, ev.CompanyId);
        Assert.Equal(22, ev.SiteId);

        var payload = JsonDocument.Parse(ev.PayloadJson).RootElement;
        Assert.Equal("Upsert", payload.GetProperty("action").GetString());
        Assert.Equal(nameof(AccessLog), payload.GetProperty("entityType").GetString());
        Assert.True(payload.TryGetProperty("entity", out _));
    }

    [Fact]
    public void BuildCandidates_Central_PublishedCanonical()
    {
        var db = CreateDb();
        db.Employees.Add(new Employee { FullName = "Khôi" });
        var result = Factory(SyncRuntimeModes.Central).BuildCandidates(db.ChangeTracker);

        var candidate = Assert.Single(result);
        var ev = candidate.OutboxEvent;
        Assert.Equal("Published", ev.Status);
        Assert.Equal("Central", ev.SourceSystem);
        Assert.True(ev.IsCanonical);
        Assert.Null(ev.NextAttemptAtUtc);
        Assert.Null(ev.AreaNodeId);
    }

    [Fact]
    public void BuildCandidates_ScopesToGate()
    {
        var db = CreateDb();
        var gate = new Gate { GateId = 5, GateName = "A" };
        db.Gates.Add(gate);
        var result = Factory(SyncRuntimeModes.Central).BuildCandidates(db.ChangeTracker);

        var candidate = Assert.Single(result);
        Assert.Equal("Gate", candidate.OutboxEvent.ScopeType);
        Assert.Equal(5, candidate.OutboxEvent.ScopeId);
    }

    [Fact]
    public void BuildCandidates_Delete_SetsDeleteAction()
    {
        var db = CreateDb();
        var gate = new Gate { GateId = 5, GateName = "A" };
        db.Gates.Add(gate);
        db.SaveChanges();
        db.Gates.Remove(gate);

        var result = Factory(SyncRuntimeModes.Central).BuildCandidates(db.ChangeTracker);

        var candidate = Assert.Single(result);
        var ev = candidate.OutboxEvent;
        Assert.Equal("Sync.Gate.Delete", ev.EventType);
        Assert.Equal("5", ev.AggregateId);
        var payload = JsonDocument.Parse(ev.PayloadJson).RootElement;
        Assert.Equal("Delete", payload.GetProperty("action").GetString());
        Assert.Equal("5", payload.GetProperty("keys").GetProperty("GateId").GetInt32().ToString());
    }

    [Fact]
    public void BuildCandidates_SkipsSystemTables()
    {
        var db = CreateDb();
        db.SystemAuditLogs.Add(new SystemAuditLog { ActionType = "x", Username = "y", TimestampUtc = DateTime.UtcNow });
        db.SyncAreaNodes.Add(new SyncAreaNode { AreaNodeId = "n", DisplayName = "n", CreatedAtUtc = DateTime.UtcNow });
        var result = Factory(SyncRuntimeModes.Central).BuildCandidates(db.ChangeTracker);
        Assert.Empty(result);
    }
}