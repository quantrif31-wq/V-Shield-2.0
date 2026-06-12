using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("ImportExportHistory")]
public class ImportExportHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string OperationType { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = null!;

    [MaxLength(255)]
    public string FileName { get; set; } = null!;

    [MaxLength(10)]
    public string FileFormat { get; set; } = null!;

    public long FileSize { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }

    public string? ErrorDetails { get; set; }

    public byte[]? OriginalFileContent { get; set; }

    public byte[]? ResultFileContent { get; set; }

    public int? PerformedById { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    public long? DurationMs { get; set; }

    public string? AdditionalInfo { get; set; }

    [ForeignKey("PerformedById")]
    public virtual AppUser? PerformedBy { get; set; }
}
