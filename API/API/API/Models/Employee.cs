using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Models;

[Table("Employee")]
public partial class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    public int? DepartmentId { get; set; }

    public int? PositionId { get; set; }

    [StringLength(150)]
    public string FullName { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [Column("FaceImageURL")]
    [StringLength(300)]
    public string? FaceImageUrl { get; set; }

    public bool? Status { get; set; }

    [StringLength(40)]
    public string LifecycleStatus { get; set; } = EmployeeLifecycleStates.Active;

    public int? PrimarySiteId { get; set; }

    public int? ManagerEmployeeId { get; set; }

    public DateTime? LifecycleUpdatedAtUtc { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();

    [ForeignKey("DepartmentId")]
    [InverseProperty("Employees")]
    public virtual Department? Department { get; set; }

    [ForeignKey("PositionId")]
    [InverseProperty("Employees")]
    public virtual Position? Position { get; set; }

    [InverseProperty("HostEmployee")]
    public virtual ICollection<PreRegistration> PreRegistrations { get; set; } = new List<PreRegistration>();

    [InverseProperty("Employee")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    // Navigation ngược: một Employee có thể có một tài khoản AppUser
    [InverseProperty("Employee")]
    public virtual AppUser? AppUser { get; set; }

    [ForeignKey("PrimarySiteId")]
    public virtual Site? PrimarySite { get; set; }

    [ForeignKey("ManagerEmployeeId")]
    public virtual Employee? ManagerEmployee { get; set; }

    public virtual ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    [InverseProperty("FromEmployee")]
    public virtual ICollection<VehicleDelegation> OutgoingDelegations { get; set; } = new List<VehicleDelegation>();

    [InverseProperty("ToEmployee")]
    public virtual ICollection<VehicleDelegation> IncomingDelegations { get; set; } = new List<VehicleDelegation>();
}
