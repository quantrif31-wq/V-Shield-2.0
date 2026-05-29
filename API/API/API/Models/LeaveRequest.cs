using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("LeaveRequests")]
public class LeaveRequest
{
    [Key]
    public int LeaveRequestId { get; set; }

    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(30)]
    public string LeaveType { get; set; } = LeaveTypes.AnnualLeave;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = LeaveRequestStatuses.Pending;

    public int? ApproverId { get; set; }

    [MaxLength(1000)]
    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(ApproverId))]
    public virtual AppUser? Approver { get; set; }
}

