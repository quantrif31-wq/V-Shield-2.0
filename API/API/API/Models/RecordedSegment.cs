using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("Recorded_Segment")]
public class RecordedSegment
{
    [Key]
    public long SegmentId { get; set; }

    public int CameraId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public double DurationSeconds { get; set; }

    [StringLength(500)]
    public string StorageUrl { get; set; } = string.Empty;

    [ForeignKey("CameraId")]
    public virtual Camera? Camera { get; set; }
}
