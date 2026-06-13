using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class AiFeedback
{
    public long Id { get; set; }

    public long RecommendationId { get; set; }

    public int UserId { get; set; }

    [MaxLength(40)]
    public string FeedbackType { get; set; } = "Useful";

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
