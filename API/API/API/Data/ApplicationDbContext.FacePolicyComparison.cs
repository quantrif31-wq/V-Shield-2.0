using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<FaceAccessPolicyComparison> FaceAccessPolicyComparisons { get; set; }

    private static void ConfigureFacePolicyComparisons(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FaceAccessPolicyComparison>();
        entity.HasIndex(x => x.FaceRecognitionEventId).IsUnique();
        entity.HasIndex(x => x.OccurredAtUtc);
        entity.HasIndex(x => x.ComparisonResult);
        entity.HasIndex(x => x.LegacyDecision);
        entity.HasIndex(x => x.EnterpriseDecision);
        entity.HasIndex(x => x.EmployeeId);
        entity.HasIndex(x => x.GateId);
        entity.HasIndex(x => x.AccessPointId);
        entity.HasOne(x => x.FaceRecognitionEvent).WithMany()
            .HasForeignKey(x => x.FaceRecognitionEventId).OnDelete(DeleteBehavior.Restrict);
    }
}
