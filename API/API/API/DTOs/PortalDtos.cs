using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class PortalOverviewDto
{
    public string SystemName { get; set; } = "V-SHIELD 2.0";
    public string Tagline { get; set; } = "Hệ thống kiểm soát an ninh thông minh đa nền tảng & AI Realtime";
    public string Version { get; set; } = "2.0.0";
    public string ReleaseDate { get; set; } = "2026-08-31";
    public string BuildNumber { get; set; } = "2026.08.FINAL";
    public double AverageRating { get; set; } = 4.95;
    public int TotalReviews { get; set; } = 1280;
    public int TotalComments { get; set; } = 3450;
    public int ActiveDeployments { get; set; } = 3;
    public string ServerStatus { get; set; } = "Online";
    public string PublicUrl { get; set; } = "https://v-shield.site";
    public string ApkDownloadUrl { get; set; } = "/downloads/VShield-Mobile-Latest.apk";
    public string ApkVersion { get; set; } = "2.0.0";
    public long ApkSizeBytes { get; set; } = 61059982;
    public List<string> SupportedPlatforms { get; set; } = new() { "Web Cloud (VPS)", "Local Station (Docker)", "Mobile Android (APK)" };
}

public class PortalReviewDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorRole { get; set; }
    public string? AvatarUrl { get; set; }
    public int Rating { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; } = true;
    public int LikesCount { get; set; } = 0;
}

public class CreatePortalReviewRequest
{
    [Required, StringLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required, StringLength(1000, MinimumLength = 5)]
    public string Content { get; set; } = string.Empty;

    public string? Platform { get; set; } = "Web";
}

public class PortalCommentDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Badge { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int LikesCount { get; set; } = 0;
    public List<PortalCommentReplyDto> Replies { get; set; } = new();
}

public class PortalCommentReplyDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Badge { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CreatePortalCommentRequest
{
    [Required, StringLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required, StringLength(1500, MinimumLength = 3)]
    public string Content { get; set; } = string.Empty;

    public string? ParentCommentId { get; set; }
}

public class PortalFeedbackRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required, StringLength(50)]
    public string Category { get; set; } = "Feature"; // Feature, Bug, UX, Partnership

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}

public class PortalNewsletterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }
}

public class CommunityGoogleAuthRequest
{
    [Required]
    public string GoogleTokenOrEmail { get; set; } = string.Empty;

    public string? FullName { get; set; }
    public string? PhotoUrl { get; set; }
}

public class CommunityUserProfileDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Role { get; set; } = "Community Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string Token { get; set; } = string.Empty;
}
