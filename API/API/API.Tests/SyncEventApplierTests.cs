using System.Reflection;
using System.Text.Json;
using API.Data;
using API.Models;
using API.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class SyncEventApplierTests
{
    private enum TestDirection { In, Out }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"sea_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static SyncEventApplier CreateApplier(ApplicationDbContext db) => new(db);

    private static SyncEventDto Event(string aggregateType, string payload, string? aggregateId = null, string sourceSystem = "AreaNode") =>
        new(
            EventType: "entity.updated",
            AggregateType: aggregateType,
            AggregateId: aggregateId,
            CorrelationId: Guid.NewGuid().ToString("N"),
            PayloadJson: payload,
            CompanyId: null,
            SiteId: null,
            AreaNodeId: "node-1",
            ScopeType: null,
            ScopeId: null,
            SourceSystem: sourceSystem,
            SchemaVersion: 1,
            OccurredAtUtc: DateTime.UtcNow);

    private static T InvokeStatic<T>(string methodName, params object[] args)
    {
        var method = typeof(SyncEventApplier).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static, null, args.Select(a => a.GetType()).ToArray(), null)!;
        return (T)method.Invoke(null, args)!;
    }

    private static JsonElement Elem(string json, string prop = "value")
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty(prop).Clone();
    }

    private static JsonElement Root(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static Dictionary<string, JsonElement> Fields(JsonElement entity) =>
        entity.ValueKind == JsonValueKind.Object
            ? entity.EnumerateObject().ToDictionary(i => i.Name, i => i.Value, StringComparer.OrdinalIgnoreCase)
            : [];

    [Fact]
    public async Task ApplyAsync_UnknownAggregate_ReturnsNull()
    {
        var db = CreateDb();
        var applier = CreateApplier(db);
        var result = await applier.ApplyAsync(Event("UnknownThing", "{}"));
        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_Gate_InsertAreaNode_CreatesRow()
    {
        var db = CreateDb();
        var applier = CreateApplier(db);
        var result = await applier.ApplyAsync(Event(nameof(Gate), "{\"action\":\"Upsert\",\"entity\":{\"GateName\":\"Cổng A\",\"Location\":\"Tầng 1\"}}"));

        var gate = Assert.Single(db.Gates);
        Assert.Equal("Cổng A", gate.GateName);
        Assert.Equal("Tầng 1", gate.Location);
        Assert.Equal(gate.GateId.ToString(), result);
    }

    [Fact]
    public async Task ApplyAsync_Gate_UpdateExisting_ModifiesRow()
    {
        var db = CreateDb();
        db.Gates.Add(new Gate { GateId = 5, GateName = "Cổng A", Location = "Tầng 1" });
        db.SaveChanges();
        var applier = CreateApplier(db);

        var result = await applier.ApplyAsync(Event(nameof(Gate),
            "{\"action\":\"Upsert\",\"entity\":{\"GateName\":\"Cổng B\"}}", aggregateId: "5"));

        Assert.Equal("5", result);
        var gate = Assert.Single(db.Gates);
        Assert.Equal("Cổng B", gate.GateName);
        Assert.Equal("Tầng 1", gate.Location);
        Assert.Equal(5, gate.GateId);
    }

    [Fact]
    public async Task ApplyAsync_Gate_Delete_RemovesRow()
    {
        var db = CreateDb();
        db.Gates.Add(new Gate { GateId = 5, GateName = "Cổng A" });
        db.SaveChanges();
        var applier = CreateApplier(db);

        var result = await applier.ApplyAsync(Event(nameof(Gate),
            "{\"action\":\"Delete\",\"entity\":{\"GateName\":\"Cổng A\"}}", aggregateId: "5"));

        Assert.Equal("5", result);
        Assert.Empty(db.Gates);
    }

    [Fact]
    public void IsScalarProperty_ClassifiesTypes()
    {
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(int)));
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(int?)));
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(string)));
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(decimal)));
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(DateTime)));
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(Guid)));
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(bool)));
        Assert.True(InvokeStatic<bool>("IsScalarProperty", typeof(TestDirection)));
        Assert.False(InvokeStatic<bool>("IsScalarProperty", typeof(List<int>)));
    }

    [Fact]
    public void ConvertJsonValue_ConvertsCommonTypes()
    {
        Assert.Null(InvokeStatic<object?>("ConvertJsonValue", Elem("{\"value\":null}"), typeof(string)));
        Assert.Equal("abc", InvokeStatic<string?>("ConvertJsonValue", Elem("{\"value\":\"abc\"}"), typeof(string)));
        Assert.Equal(42, InvokeStatic<int?>("ConvertJsonValue", Elem("{\"value\":42}"), typeof(int)));
        Assert.Equal(42, InvokeStatic<int?>("ConvertJsonValue", Elem("{\"value\":\"42\"}"), typeof(int)));
        Assert.Equal(42L, InvokeStatic<long?>("ConvertJsonValue", Elem("{\"value\":42}"), typeof(long)));
        Assert.True(InvokeStatic<bool?>("ConvertJsonValue", Elem("{\"value\":true}"), typeof(bool)));
        Assert.True(InvokeStatic<bool?>("ConvertJsonValue", Elem("{\"value\":\"true\"}"), typeof(bool)));
        Assert.Equal(10.5m, InvokeStatic<decimal?>("ConvertJsonValue", Elem("{\"value\":10.5}"), typeof(decimal)));
        Assert.Equal(Guid.Parse("a99b2f70-1f5e-4b3d-9c2e-111111111111"), InvokeStatic<Guid?>("ConvertJsonValue", Elem("{\"value\":\"a99b2f70-1f5e-4b3d-9c2e-111111111111\"}"), typeof(Guid)));
        Assert.Equal(TestDirection.In, InvokeStatic<TestDirection>("ConvertJsonValue", Elem("{\"value\":\"In\"}"), typeof(TestDirection)));
        Assert.Null(InvokeStatic<object?>("ConvertJsonValue", Elem("{\"value\":\"2024-01-01T00:00:00Z\"}"), typeof(List<int>)));
    }

    [Fact]
    public void ConvertJsonValue_DateTimeFromString()
    {
        var value = InvokeStatic<DateTime>("ConvertJsonValue",
            Elem("{\"value\":\"2024-05-06T07:08:09Z\"}"), typeof(DateTime));
        Assert.Equal(new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc), value.ToUniversalTime());
    }

    [Fact]
    public void TryGetHelpers_ParseFromObjectElement()
    {
        using var doc = JsonDocument.Parse("{\"num\":5,\"txt\":\"hello\",\"bad\":\"nope\"}");
        var entity = doc.RootElement;

        Assert.Equal(5, InvokeStatic<int?>("TryGetInt", entity, "num"));
        Assert.Null(InvokeStatic<int?>("TryGetInt", entity, "missing"));
        Assert.Null(InvokeStatic<int?>("TryGetInt", entity, "txt"));
        Assert.Equal(5L, InvokeStatic<long?>("TryGetLong", entity, "num"));
        Assert.Null(InvokeStatic<long?>("TryGetLong", entity, "bad"));
    }

    [Fact]
    public void TryGetHelpers_FromFieldDictionary()
    {
        var entity = Root("{\"a\":1,\"b\":\"x\",\"d\":\"2024-01-02T03:04:05Z\"}");
        var fields = Fields(entity);

        Assert.Equal(1, InvokeStatic<int?>("TryGetInt", fields, "a"));
        Assert.Equal("x", InvokeStatic<string?>("TryGetString", fields, "b"));
        Assert.Null(InvokeStatic<string?>("TryGetString", fields, "missing"));
        Assert.Null(InvokeStatic<int?>("TryGetInt", fields, "b"));
        Assert.NotNull(InvokeStatic<DateTime?>("TryGetDateTime", fields, "d"));
        Assert.Null(InvokeStatic<DateTime?>("TryGetDateTime", fields, "missing"));
    }

    [Fact]
    public void BuildCentralConversationMappingKey_Formats()
    {
        var key = InvokeStatic<string>("BuildCentralConversationMappingKey", "42");
        Assert.Equal("sync.chatconversation.map.central.42", key);
    }

    [Fact]
    public void ApplyScalarValues_SetsScalarAndSkipsNonScalar()
    {
        var entity = Root("{\"gateid\":\"7\",\"GateName\":\"X\",\"Location\":\"L\",\"Latitude\":10.5,\"AccessLogs\":{}}");
        var gate = new Gate();

        InvokeStatic<object?>("ApplyScalarValues", gate, entity, true);

        Assert.Equal(7, gate.GateId);
        Assert.Equal("X", gate.GateName);
        Assert.Equal("L", gate.Location);
        Assert.Equal(10.5m, gate.Latitude);
    }

    [Fact]
    public void ApplyScalarValues_WithoutPrimaryKey_SkipsKey()
    {
        var entity = Root("{\"GateId\":7,\"GateName\":\"X\"}");
        var gate = new Gate();

        InvokeStatic<object?>("ApplyScalarValues", gate, entity, false);

        Assert.Equal(0, gate.GateId);
        Assert.Equal("X", gate.GateName);
    }
}