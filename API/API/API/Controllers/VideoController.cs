using System.Security.Claims;
using API.Data;
using System.IdentityModel.Tokens.Jwt;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("ops")]
    [Authorize]
    [RequireOperationalTask("monitoring")]
    public class VideoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VideoController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadVideo([FromForm] UploadVideoRequest request)
        {
            try
            {
                var file = request.File;
                var employeeId = request.EmployeeId;

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Không có file" });

                if (file.Length > 50 * 1024 * 1024)
                    return BadRequest(new { message = "Video quá lớn, tối đa 50MB" });

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".mp4", ".mov", ".avi" };

                if (!allowed.Contains(ext))
                    return BadRequest(new { message = "Sai định dạng video" });

                var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                                  User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Token không hợp lệ" });

                var user = await _context.AppUsers
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return Unauthorized(new { message = "Không tìm thấy user" });

                int finalEmployeeId;

                if (user.Role == "Admin")
                {
                    if (employeeId == null)
                        return BadRequest(new { message = "Admin phải chọn nhân viên" });

                    var employeeExists = await _context.Employees
                        .AnyAsync(e => e.EmployeeId == employeeId.Value);

                    if (!employeeExists)
                        return BadRequest(new { message = "Nhân viên không tồn tại" });

                    finalEmployeeId = employeeId.Value;
                }
                else
                {
                    if (user.Employee == null)
                        return BadRequest(new { message = "User chưa liên kết với nhân viên" });

                    finalEmployeeId = user.Employee.EmployeeId;
                }

                string folder = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "VideoFace",
                    "video_notok"
                );

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = $"emp_{finalEmployeeId}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
                string fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var video = new EmployeeFaceVideo
                {
                    EmployeeId = finalEmployeeId,
                    FileName = fileName,
                    FilePath = "/uploads/VideoFace/video_notok/" + fileName,
                    FileSize = file.Length,
                    CreatedAt = DateTime.Now
                };

                _context.EmployeeFaceVideos.Add(video);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Upload thành công",
                    filePath = video.FilePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Upload thất bại",
                    error = ex.Message
                });
            }
        }

        [HttpGet("employee/{employeeId}")]
        [Authorize(Roles = "Admin,BaoVe")]
        public async Task<IActionResult> GetVideosByEmployee(int employeeId)
        {
            var videos = await _context.EmployeeFaceVideos
                .Where(v => v.EmployeeId == employeeId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return Ok(videos);
        }

        [HttpGet("{id}/content")]
        [Authorize(Roles = "Admin,BaoVe")]
        public async Task<IActionResult> GetVideoContent(int id)
        {
            var video = await _context.EmployeeFaceVideos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);

            if (video == null)
                return NotFound(new { message = "Khong tim thay video" });

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var videoFolder = Path.Combine(webRoot, "uploads", "VideoFace", "video_notok");
            var fullPath = Path.GetFullPath(Path.Combine(videoFolder, video.FileName));
            var allowedRoot = Path.GetFullPath(videoFolder);

            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
                return NotFound(new { message = "Khong tim thay file video" });

            await AuditEvidenceRead("EmployeeFaceVideo", id.ToString(), video.FileName);
            return PhysicalFile(fullPath, ResolveVideoContentType(fullPath), enableRangeProcessing: true);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            try
            {
                var video = await _context.EmployeeFaceVideos.FindAsync(id);

                if (video == null)
                    return NotFound(new { message = "Không tìm thấy video" });

                string fullPath = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "VideoFace",
                    "video_notok",
                    video.FileName
                );

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                _context.EmployeeFaceVideos.Remove(video);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa video thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Xóa video thất bại",
                    error = ex.Message
                });
            }
        }

        private async Task AuditEvidenceRead(string entityName, string entityId, string fileName)
        {
            var userIdRaw = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var username = User.Identity?.Name
                ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? userIdRaw;

            _context.SystemAuditLogs.Add(new SystemAuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                UserId = int.TryParse(userIdRaw, out var userId) ? userId : null,
                Username = username,
                HttpMethod = HttpContext.Request.Method,
                Path = HttpContext.Request.Path.Value,
                ActionType = "READ",
                EntityName = entityName,
                EntityId = entityId,
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    fileName
                }),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            });
            await _context.SaveChangesAsync();
        }

        private static string ResolveVideoContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                _ => "application/octet-stream"
            };
        }
    }
}
