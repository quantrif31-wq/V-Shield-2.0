using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Models;

[Table("Camera")]
public partial class Camera
{
    [Key]
    public int CameraId { get; set; }

    [StringLength(100)]
    public string CameraName { get; set; } = null!;

    public int? GateId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CameraType { get; set; }
    [StringLength(500)]
    public string? StreamUrl { get; set; }
    [StringLength(500)]
    public string? UrlView { get; set; }

    public bool IsRecordingEnabled { get; set; }

    public int RecordingRetentionDays { get; set; } = 30;

    [InverseProperty("Camera")]
    public virtual ICollection<RecordedSegment> RecordedSegments { get; set; } = new List<RecordedSegment>();

    [InverseProperty("Camera")]
    public virtual ICollection<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();

    [ForeignKey("GateId")]
    [InverseProperty("Cameras")]
    public virtual Gate? Gate { get; set; }

    public virtual FaceCameraConfiguration? FaceCameraConfiguration { get; set; }
}
