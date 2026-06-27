namespace API.DTOs
{

    public class SetPermissionRequest
    {
        public int? EmployeeId { get; set; }
        public int? VisitorDetailId { get; set; }
        public int GateId { get; set; }
        public bool IsAllowed { get; set; }
    }

    public class QrScanAccessRequest
    {
        public string? QrPayload { get; set; }
        public int? EmployeeId { get; set; }
        public int? VisitorDetailId { get; set; }
        public int CameraId { get; set; }
        public string? QrSnapshotBase64 { get; set; }
        public string? FaceSnapshotBase64 { get; set; }
    }

    public class ManualAccessRequest
    {
        public int? EmployeeId { get; set; }
        public int? VisitorDetailId { get; set; }
        public int GateId { get; set; }
        public string? Reason { get; set; }
        public bool IsDenied { get; set; }
        public string? FaceSnapshotBase64 { get; set; }
    }
}
