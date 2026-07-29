using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<FaceAccessDecision> FaceAccessDecisions { get; set; }

    private static void ConfigureFaceAccessDecisions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FaceAccessDecision>();
        entity.HasIndex(x => x.FaceRecognitionEventId).IsUnique();
        entity.HasIndex(x => x.FaceAccessPolicyComparisonId).IsUnique();
        entity.HasIndex(x => x.OccurredAtUtc);
        entity.HasIndex(x => x.Decision);
        entity.HasIndex(x => x.EmployeeId);
        entity.HasIndex(x => x.GateId);
        entity.HasIndex(x => x.AccessPointId);
        entity.Property(x => x.PolicySnapshotJson).HasColumnType("nvarchar(max)");
        entity.HasOne(x => x.FaceRecognitionEvent).WithMany()
            .HasForeignKey(x => x.FaceRecognitionEventId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(x => x.FaceAccessPolicyComparison).WithMany()
            .HasForeignKey(x => x.FaceAccessPolicyComparisonId).OnDelete(DeleteBehavior.NoAction);
    }
}
