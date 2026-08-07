using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Models;

[Table("Vehicle")]
[Index("LicensePlate", Name = "UQ__Vehicle__026BC15CB8D416A0", IsUnique = true)]
public partial class Vehicle
{
    [Key]
    public int VehicleId { get; set; }

    [StringLength(20)]
    public string LicensePlate { get; set; } = null!;

    public int? VehicleTypeId { get; set; }

    public int? EmployeeId { get; set; }

    public int? SiteId { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Vehicles")]
    public string ParkingStatus { get; set; } = "OUT";
    public virtual Employee? Employee { get; set; }

    [ForeignKey("SiteId")]
    public virtual Site? Site { get; set; }

    [ForeignKey("VehicleTypeId")]
    [InverseProperty("Vehicles")]
    public virtual VehicleType? VehicleType { get; set; }
    public int? VisitorDetailId { get; set; }
    public VisitorDetail? VisitorDetail { get; set; }

    [InverseProperty("Vehicle")]
    public virtual ICollection<VehicleDelegation> Delegations { get; set; } = new List<VehicleDelegation>();
}
