using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace API.Tests;

public sealed class FaceHistoricalDeleteBehaviorTests
{
    [Fact]
    public void RecognitionEvent_AllHistoricalForeignKeysUseNoAction()
    {
        using var db = Database();
        var entity = db.Model.FindEntityType(typeof(FaceRecognitionEvent))!;
        var expected = new[]
        {
            nameof(FaceRecognitionEvent.EmployeeId),
            nameof(FaceRecognitionEvent.EmployeeFaceModelId),
            nameof(FaceRecognitionEvent.FaceCameraConfigurationId),
            nameof(FaceRecognitionEvent.LaneId)
        };
        var foreignKeys = entity.GetForeignKeys().ToList();
        Assert.Equal(expected.Order(), foreignKeys.Select(ForeignKeyProperty).Order());
        Assert.All(foreignKeys, fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
    }

    [Fact]
    public void PolicyComparison_EventRelationshipUsesNoAction()
    {
        using var db = Database();
        var entity = db.Model.FindEntityType(typeof(FaceAccessPolicyComparison))!;
        var foreignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(nameof(FaceAccessPolicyComparison.FaceRecognitionEventId),
            ForeignKeyProperty(foreignKey));
        Assert.Equal(DeleteBehavior.NoAction, foreignKey.DeleteBehavior);
        Assert.True(entity.GetIndexes().Single(index =>
            index.Properties.Single().Name ==
            nameof(FaceAccessPolicyComparison.FaceRecognitionEventId)).IsUnique);
    }

    [Fact]
    public void AccessCredential_OwnershipRelationshipsNeverCascade()
    {
        using var db = Database();
        var entity = db.Model.FindEntityType(typeof(AccessCredential))!;
        var foreignKeys = entity.GetForeignKeys().ToList();
        var ownership = foreignKeys.Where(fk => ForeignKeyProperty(fk) is
            nameof(AccessCredential.EmployeeId) or
            nameof(AccessCredential.EmployeeDynamicQrId)).ToList();
        Assert.Equal(2, ownership.Count);
        Assert.All(foreignKeys, fk =>
            Assert.Contains(fk.DeleteBehavior, new[] { DeleteBehavior.Restrict, DeleteBehavior.NoAction }));
    }

    [Fact]
    public async Task DeletingComparisonDoesNotMutateRecognitionEvent()
    {
        await using var db = Database();
        var eventRow = new FaceRecognitionEvent
        {
            Id = 1, RuntimeEventId = Guid.NewGuid(), CameraId = "history-camera",
            EventType = "Recognized", MatchStatus = FaceRecognitionMatchStatuses.Matched,
            OccurredAtUtc = DateTime.UtcNow, ReceivedAtUtc = DateTime.UtcNow
        };
        db.FaceRecognitionEvents.Add(eventRow);
        db.FaceAccessPolicyComparisons.Add(new FaceAccessPolicyComparison
        {
            Id = 1, FaceRecognitionEventId = 1, FaceRecognitionEvent = eventRow,
            CameraId = "history-camera", OccurredAtUtc = eventRow.OccurredAtUtc,
            LegacyDecision = "Allow", LegacyReasonCode = "test",
            EnterpriseDecision = "Indeterminate", EnterpriseReasonCode = "test",
            ComparisonResult = "test", MappingStatus = "test",
            LegacyInputFingerprint = new string('a', 64),
            EnterpriseInputFingerprint = new string('b', 64),
            ScheduleTimeZoneId = "UTC"
        });
        await db.SaveChangesAsync();
        db.FaceAccessPolicyComparisons.Remove(await db.FaceAccessPolicyComparisons.SingleAsync());
        await db.SaveChangesAsync();
        Assert.Single(await db.FaceRecognitionEvents.AsNoTracking().ToListAsync());
        Assert.Empty(await db.FaceAccessPolicyComparisons.AsNoTracking().ToListAsync());
    }

    private static string ForeignKeyProperty(IForeignKey foreignKey) =>
        foreignKey.Properties.Single().Name;

    private static ApplicationDbContext Database() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"historical-delete-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)).Options);
}
