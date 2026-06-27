namespace API.DTOs
{
    public class ToggleRecordingRequest
    {
        public bool Enabled { get; set; }
        public int? RetentionDays { get; set; }
    }
}
