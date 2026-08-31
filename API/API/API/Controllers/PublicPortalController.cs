using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/portal")]
[AllowAnonymous]
public class PublicPortalController : ControllerBase
{
    private readonly IPublicPortalService _portalService;
    private readonly IWebHostEnvironment _env;

    public PublicPortalController(IPublicPortalService portalService, IWebHostEnvironment env)
    {
        _portalService = portalService;
        _env = env;
    }

    /// <summary>Lấy thông tin tổng quan hệ thống & thống kê</summary>
    [HttpGet("overview")]
    public IActionResult GetOverview()
    {
        var overview = _portalService.GetOverview();
        return Ok(overview);
    }

    /// <summary>Lấy danh sách đánh giá cộng đồng</summary>
    [HttpGet("reviews")]
    public IActionResult GetReviews()
    {
        var reviews = _portalService.GetReviews();
        return Ok(reviews);
    }

    /// <summary>Gửi đánh giá sao mới</summary>
    [HttpPost("reviews")]
    public IActionResult CreateReview([FromBody] CreatePortalReviewRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var review = _portalService.AddReview(request);
        return Ok(new { success = true, message = "Cảm ơn bạn đã gửi đánh giá!", data = review });
    }

    /// <summary>Lấy danh sách bình luận cộng đồng</summary>
    [HttpGet("comments")]
    public IActionResult GetComments()
    {
        var comments = _portalService.GetComments();
        return Ok(comments);
    }

    /// <summary>Đăng bình luận mới</summary>
    [HttpPost("comments")]
    public IActionResult CreateComment([FromBody] CreatePortalCommentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var comment = _portalService.AddComment(request);
        return Ok(new { success = true, message = "Bình luận đã được đăng thành công!", data = comment });
    }

    /// <summary>Thả cảm xúc / Thích bình luận</summary>
    [HttpPost("comments/{id}/react")]
    public IActionResult ReactComment([FromRoute] string id, [FromQuery] string type = "like")
    {
        var success = _portalService.ReactComment(id, type);
        if (!success)
            return NotFound(new { success = false, message = "Không tìm thấy bình luận" });

        return Ok(new { success = true, message = "Đã ghi nhận cảm xúc!" });
    }

    /// <summary>Gửi góp ý tính năng hoặc báo lỗi</summary>
    [HttpPost("feedback")]
    public IActionResult SubmitFeedback([FromBody] PortalFeedbackRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _portalService.SubmitFeedback(request);
        return Ok(new { success = true, message = "Góp ý của bạn đã được gửi trực tiếp tới đội ngũ phát triển V-Shield 2.0!" });
    }

    /// <summary>Đăng ký nhận bản tin & thông báo qua email</summary>
    [HttpPost("newsletter")]
    public IActionResult SubscribeNewsletter([FromBody] PortalNewsletterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _portalService.SubscribeNewsletter(request);
        return Ok(new { success = true, message = "Đăng ký nhận tin tức thành công!" });
    }

    /// <summary>Đăng nhập / Đăng ký cộng đồng qua Google hoặc Email</summary>
    [HttpPost("auth/google")]
    public IActionResult AuthenticateGoogle([FromBody] CommunityGoogleAuthRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var profile = _portalService.AuthenticateCommunityUser(request);
        return Ok(new { success = true, message = "Đăng nhập cộng đồng thành công!", data = profile });
    }

    /// <summary>Tải trực tiếp file Mobile APK</summary>
    [HttpGet("download/apk")]
    public IActionResult DownloadApk()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads", "VShield-Mobile-Latest.apk"),
            Path.Combine(Directory.GetCurrentDirectory(), "VShield-Mobile-Latest.apk"),
            Path.Combine(_env.ContentRootPath, "..", "..", "..", "VShield-Mobile-Latest.apk")
        };

        foreach (var p in possiblePaths)
        {
            if (System.IO.File.Exists(p))
            {
                var stream = System.IO.File.OpenRead(p);
                return File(stream, "application/vnd.android.package-archive", "VShield-Mobile-Latest.apk");
            }
        }

        return Redirect("https://v-shield.site/downloads/VShield-Mobile-Latest.apk");
    }
}
