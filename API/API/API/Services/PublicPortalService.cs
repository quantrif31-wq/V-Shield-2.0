using System.Collections.Concurrent;
using API.DTOs;

namespace API.Services;

public interface IPublicPortalService
{
    PortalOverviewDto GetOverview();
    List<PortalReviewDto> GetReviews();
    PortalReviewDto AddReview(CreatePortalReviewRequest request);
    List<PortalCommentDto> GetComments();
    PortalCommentDto AddComment(CreatePortalCommentRequest request);
    bool ReactComment(string commentId, string reactionType);
    bool SubmitFeedback(PortalFeedbackRequest request);
    bool SubscribeNewsletter(PortalNewsletterRequest request);
    CommunityUserProfileDto AuthenticateCommunityUser(CommunityGoogleAuthRequest request);
}

public class PublicPortalService : IPublicPortalService
{
    private static readonly ConcurrentBag<PortalReviewDto> _reviews = new();
    private static readonly ConcurrentBag<PortalCommentDto> _comments = new();
    private static readonly ConcurrentBag<PortalFeedbackRequest> _feedbackList = new();
    private static readonly ConcurrentBag<PortalNewsletterRequest> _newsletterList = new();
    private static readonly ConcurrentDictionary<string, CommunityUserProfileDto> _communityUsers = new();

    static PublicPortalService()
    {
        SeedInitialData();
    }

    public PortalOverviewDto GetOverview()
    {
        var reviews = _reviews.ToList();
        var avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 4.95;

        return new PortalOverviewDto
        {
            AverageRating = Math.Round(avgRating, 2),
            TotalReviews = Math.Max(reviews.Count, 1280),
            TotalComments = Math.Max(_comments.Count, 3450),
            ActiveDeployments = 3,
            ServerStatus = "Online",
            ReleaseDate = "2026-08-31",
            Version = "2.0.0",
            BuildNumber = "2026.08.FINAL"
        };
    }

    public List<PortalReviewDto> GetReviews()
    {
        return _reviews.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public PortalReviewDto AddReview(CreatePortalReviewRequest request)
    {
        var review = new PortalReviewDto
        {
            Id = Guid.NewGuid().ToString(),
            AuthorName = request.AuthorName,
            AuthorRole = "Thành viên cộng đồng",
            AvatarUrl = $"https://api.dicebear.com/7.x/bottts/svg?seed={Uri.EscapeDataString(request.AuthorName)}",
            Rating = Math.Clamp(request.Rating, 1, 5),
            Content = request.Content,
            Platform = request.Platform ?? "Web",
            CreatedAt = DateTime.UtcNow,
            IsVerified = true,
            LikesCount = 1
        };

        _reviews.Add(review);
        return review;
    }

    public List<PortalCommentDto> GetComments()
    {
        return _comments.OrderByDescending(c => c.CreatedAt).ToList();
    }

    public PortalCommentDto AddComment(CreatePortalCommentRequest request)
    {
        if (!string.IsNullOrEmpty(request.ParentCommentId))
        {
            var parent = _comments.FirstOrDefault(c => c.Id == request.ParentCommentId);
            if (parent != null)
            {
                var reply = new PortalCommentReplyDto
                {
                    Id = Guid.NewGuid().ToString(),
                    AuthorName = request.AuthorName,
                    AvatarUrl = $"https://api.dicebear.com/7.x/adventurer/svg?seed={Uri.EscapeDataString(request.AuthorName)}",
                    Badge = "Thành viên",
                    Content = request.Content,
                    CreatedAt = DateTime.UtcNow
                };
                lock (parent.Replies)
                {
                    parent.Replies.Add(reply);
                }
                return parent;
            }
        }

        var comment = new PortalCommentDto
        {
            Id = Guid.NewGuid().ToString(),
            AuthorName = request.AuthorName,
            AvatarUrl = $"https://api.dicebear.com/7.x/adventurer/svg?seed={Uri.EscapeDataString(request.AuthorName)}",
            Badge = "Operator",
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            LikesCount = 0,
            Replies = new List<PortalCommentReplyDto>()
        };

        _comments.Add(comment);
        return comment;
    }

    public bool ReactComment(string commentId, string reactionType)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == commentId);
        if (comment != null)
        {
            lock (comment)
            {
                comment.LikesCount++;
            }
            return true;
        }
        return false;
    }

    public bool SubmitFeedback(PortalFeedbackRequest request)
    {
        _feedbackList.Add(request);
        return true;
    }

    public bool SubscribeNewsletter(PortalNewsletterRequest request)
    {
        _newsletterList.Add(request);
        return true;
    }

    public CommunityUserProfileDto AuthenticateCommunityUser(CommunityGoogleAuthRequest request)
    {
        var email = request.GoogleTokenOrEmail.Contains('@')
            ? request.GoogleTokenOrEmail.Trim().ToLowerInvariant()
            : $"user_{Guid.NewGuid().ToString()[..8]}@gmail.com";

        var fullName = !string.IsNullOrWhiteSpace(request.FullName)
            ? request.FullName.Trim()
            : (email.Split('@')[0]);

        var avatar = !string.IsNullOrWhiteSpace(request.PhotoUrl)
            ? request.PhotoUrl
            : $"https://api.dicebear.com/7.x/adventurer/svg?seed={Uri.EscapeDataString(fullName)}";

        return _communityUsers.GetOrAdd(email, _ => new CommunityUserProfileDto
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            FullName = fullName,
            AvatarUrl = avatar,
            Role = "Operator Member",
            JoinedAt = DateTime.UtcNow,
            Token = "COMMUNITY_OAUTH_" + Guid.NewGuid().ToString("N")
        });
    }

    private static void SeedInitialData()
    {
        // ── Seed Reviews ──
        _reviews.Add(new PortalReviewDto
        {
            Id = "rev-1",
            AuthorName = "GS.TS Nguyễn Thanh Tùng",
            AuthorRole = "Chuyên gia An ninh Thông tin",
            AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=ProfTung",
            Rating = 5,
            Content = "V-Shield 2.0 đã giải quyết triệt để bài toán điểm mù kiểm soát giao thông ra vào. Cơ chế virtual barrier kết hợp nhận diện khuôn mặt đa góc hoạt động cực kỳ mượt mà, độ trễ sub-30ms thực sự ấn tượng!",
            Platform = "Web Cloud",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            LikesCount = 42,
            IsVerified = true
        });

        _reviews.Add(new PortalReviewDto
        {
            Id = "rev-2",
            AuthorName = "Trần Hoàng Nam",
            AuthorRole = "DevOps Lead - SmartBuilding VN",
            AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=NamDev",
            Rating = 5,
            Content = "Triển khai 100% Docker Container hóa vô cùng gọn gàng và ổn định. Tính năng Offline-First Hybrid Sync tự khắc phục mất mạng cực kỳ đáng tin cậy cho môi trường doanh nghiệp quy mô lớn.",
            Platform = "Docker Local",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LikesCount = 28,
            IsVerified = true
        });

        _reviews.Add(new PortalReviewDto
        {
            Id = "rev-3",
            AuthorName = "Lê Thị Mai Chi",
            AuthorRole = "Trưởng phòng Nhân sự TechCorp",
            AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=ChiHR",
            Rating = 5,
            Content = "App Mobile V-Shield hoạt động rất tiện lợi: QR Code động bảo mật cao, đàm thoại video call trực tiếp với phòng bảo vệ nhanh chóng và giao diện đẹp mắt như ứng dụng game hiện đại.",
            Platform = "Mobile Android",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LikesCount = 35,
            IsVerified = true
        });

        // ── Seed Comments ──
        var c1 = new PortalCommentDto
        {
            Id = "cmt-1",
            AuthorName = "CyberOperator_07",
            AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=Cyber07",
            Badge = "Vanguard",
            Content = "Hệ thống AI Review đối soát phát hiện gian lận biển số xe có hỗ trợ các góc nghiêng ban đêm không các bạn?",
            CreatedAt = DateTime.UtcNow.AddHours(-18),
            LikesCount = 12,
            Replies = new List<PortalCommentReplyDto>
            {
                new()
                {
                    Id = "rep-1-1",
                    AuthorName = "Phạm Văn Thành (Dev Lead)",
                    AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=ThanhLead",
                    Badge = "Core Developer",
                    Content = "Chào bạn, mô hình YOLOv11 + OCR của V-Shield 2.0 đã được huấn luyện với tập dữ liệu ban đêm và góc nghiêng tới 45 độ kết hợp bổ trợ hồng ngoại từ Go2RTC nhé!",
                    CreatedAt = DateTime.UtcNow.AddHours(-14)
                }
            }
        };

        var c2 = new PortalCommentDto
        {
            Id = "cmt-2",
            AuthorName = "Aoi_Security",
            AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=AoiSecurity",
            Badge = "Community",
            Content = "Giao diện trang chủ anime cyberpunk này siêu đỉnh! Hiệu ứng âm thanh và hạt ánh sáng nhìn rất nghệ thuật và chuyên nghiệp.",
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            LikesCount = 19,
            Replies = new List<PortalCommentReplyDto>()
        };

        _comments.Add(c1);
        _comments.Add(c2);
    }
}
