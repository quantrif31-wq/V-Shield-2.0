using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<OperationalInterventionRequest> OperationalInterventionRequests { get; set; }

    private static void ConfigureInterventionRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OperationalInterventionRequest>(entity =>
        {
            entity.HasKey(e => e.OperationalInterventionRequestId);

            entity.Property(e => e.InterventionType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(80);

            entity.Property(e => e.LaneId).HasMaxLength(120);
            entity.Property(e => e.LaneName).HasMaxLength(160);
            entity.Property(e => e.SubjectName).HasMaxLength(240);
            entity.Property(e => e.SubjectId).HasMaxLength(80);
            entity.Property(e => e.SubjectType).HasMaxLength(40);
            entity.Property(e => e.PlateNumber).HasMaxLength(40);
            entity.Property(e => e.QrPayload).HasMaxLength(500);
            entity.Property(e => e.Note).HasMaxLength(2000);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);

            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAtUtc });

            // Navigation: RequestedByUser
            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Intervention_RequestedByUser");

            // Navigation: AcceptedByUser
            entity.HasOne(e => e.AcceptedByUser)
                .WithMany()
                .HasForeignKey(e => e.AcceptedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Intervention_AcceptedByUser");

            // Navigation: RejectedByUser
            entity.HasOne(e => e.RejectedByUser)
                .WithMany()
                .HasForeignKey(e => e.RejectedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Intervention_RejectedByUser");

            // Navigation: ExecutedByUser
            entity.HasOne(e => e.ExecutedByUser)
                .WithMany()
                .HasForeignKey(e => e.ExecutedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Intervention_ExecutedByUser");
        });
    }
}
