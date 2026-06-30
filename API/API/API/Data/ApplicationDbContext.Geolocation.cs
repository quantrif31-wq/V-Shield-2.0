using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<IndoorPathNode> IndoorPathNodes { get; set; }

    private static void ConfigureGeolocationModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndoorPathNode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(160);
            entity.Property(e => e.NodeType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.X).HasColumnType("decimal(18,6)");
            entity.Property(e => e.Y).HasColumnType("decimal(18,6)");
            entity.Property(e => e.Z).HasColumnType("decimal(18,6)");

            entity.HasOne(e => e.Building)
                  .WithMany()
                  .HasForeignKey(e => e.BuildingId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FacilityFloor)
                  .WithMany()
                  .HasForeignKey(e => e.FacilityFloorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.BuildingId, e.FacilityFloorId });
        });
    }
}
