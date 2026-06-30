using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    private static void ConfigureVehicleDelegation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleDelegation>(entity =>
        {
            entity.HasOne(e => e.FromEmployee)
                .WithMany(e => e.OutgoingDelegations)
                .HasForeignKey(e => e.FromEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ToEmployee)
                .WithMany(e => e.IncomingDelegations)
                .HasForeignKey(e => e.ToEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vehicle)
                .WithMany(e => e.Delegations)
                .HasForeignKey(e => e.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
