using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class EmployeeFaceModel
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string ModelFileName { get; set; } = string.Empty;

        public string ModelPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int? Version { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(64)]
        public string? ModelChecksum { get; set; }

        public int? EncodingCount { get; set; }

        public DateTime? ActivatedAtUtc { get; set; }

        public DateTime? ArchivedAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        [MaxLength(80)]
        public string? FailureCode { get; set; }

        [MaxLength(500)]
        public string? FailureMessage { get; set; }

        public Guid? SourceEnrollmentJobId { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Employee Employee { get; set; } = null!;
    }

    public static class FaceModelLifecycleStatuses
    {
        public const string Pending = "Pending";
        public const string Prepared = "Prepared";
        public const string Activating = "Activating";
        public const string Active = "Active";
        public const string Archived = "Archived";
        public const string Revoked = "Revoked";
        public const string Failed = "Failed";
    }
}
