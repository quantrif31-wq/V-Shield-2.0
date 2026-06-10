using API.Data;
using API.DTOs;
using API.Hubs;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<EmployeeStatsHub> _hubContext;

    public EmployeesController(ApplicationDbContext context, IHubContext<EmployeeStatsHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    /// <summary>Láº¥y danh sÃ¡ch táº¥t cáº£ nhÃ¢n viÃªn (chá»‰ Admin)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] int? positionId,
        [FromQuery] bool? status)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsQueryable();

        // Lá»c theo tÃªn hoáº·c email
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e =>
                e.FullName.Contains(search) ||
                (e.Email != null && e.Email.Contains(search)) ||
                (e.Phone != null && e.Phone.Contains(search)));

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        if (positionId.HasValue)
            query = query.Where(e => e.PositionId == positionId);

        if (status.HasValue)
            query = query.Where(e => e.Status == status);

        var employees = await query
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeResponse
            {
                EmployeeId = e.EmployeeId,
                FullName = e.FullName,
                Phone = e.Phone,
                Email = e.Email,
                FaceImageUrl = e.FaceImageUrl,
                Status = e.Status,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                PositionId = e.PositionId,
                PositionName = e.Position != null ? e.Position.Name : null
            })
            .ToListAsync();

        return Ok(employees);
    }

    /// <summary>Láº¥y thÃ´ng tin 1 nhÃ¢n viÃªn theo ID (chá»‰ Admin)</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _context.Employees
            .Include(x => x.Department)
            .Include(x => x.Position)
            .FirstOrDefaultAsync(x => x.EmployeeId == id);

        if (e == null)
            return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn ID {id}" });

        return Ok(new EmployeeResponse
        {
            EmployeeId = e.EmployeeId,
            FullName = e.FullName,
            Phone = e.Phone,
            Email = e.Email,
            FaceImageUrl = e.FaceImageUrl,
            Status = e.Status,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name,
            PositionId = e.PositionId,
            PositionName = e.Position?.Name
        });
    }

    /// <summary>Táº¡o nhÃ¢n viÃªn má»›i (chá»‰ Admin)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Kiá»ƒm tra DepartmentId há»£p lá»‡
        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.DepartmentId == request.DepartmentId))
            return BadRequest(new { message = $"DepartmentId {request.DepartmentId} khÃ´ng tá»“n táº¡i" });

        // Kiá»ƒm tra PositionId há»£p lá»‡
        if (request.PositionId.HasValue &&
            !await _context.Positions.AnyAsync(p => p.PositionId == request.PositionId))
            return BadRequest(new { message = $"PositionId {request.PositionId} khÃ´ng tá»“n táº¡i" });

        var employee = new Employee
        {
            FullName = request.FullName,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            Phone = request.Phone,
            Email = request.Email,
            FaceImageUrl = request.FaceImageUrl,
            Status = request.Status
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Load navigation properties sau khi save
        await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        await _context.Entry(employee).Reference(e => e.Position).LoadAsync();

        // Broadcast real-time update tá»›i clients Ä‘ang theo dÃµi
        int total = await _context.Employees.CountAsync();
        int active = await _context.Employees.CountAsync(e => e.Status == true);
        await _hubContext.Clients.Group("stats").SendAsync("ReceiveStatsUpdate", new EmployeeCountChangedEvent
        {
            TotalEmployees = total,
            ActiveEmployees = active,
            ChangeType = "created",
            ChangedAt = DateTime.Now
        });

        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, new EmployeeResponse
        {
            EmployeeId = employee.EmployeeId,
            FullName = employee.FullName,
            Phone = employee.Phone,
            Email = employee.Email,
            FaceImageUrl = employee.FaceImageUrl,
            Status = employee.Status,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name,
            PositionId = employee.PositionId,
            PositionName = employee.Position?.Name
        });
    }

    /// <summary>Cáº­p nháº­t nhÃ¢n viÃªn (chá»‰ Admin)</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

        if (employee == null)
            return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn ID {id}" });

        if (request.FullName != null)
            employee.FullName = request.FullName;

        if (request.DepartmentId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == request.DepartmentId))
                return BadRequest(new { message = $"DepartmentId {request.DepartmentId} khÃ´ng tá»“n táº¡i" });
            employee.DepartmentId = request.DepartmentId;
        }

        if (request.PositionId.HasValue)
        {
            if (!await _context.Positions.AnyAsync(p => p.PositionId == request.PositionId))
                return BadRequest(new { message = $"PositionId {request.PositionId} khÃ´ng tá»“n táº¡i" });
            employee.PositionId = request.PositionId;
        }

        if (request.Phone != null) employee.Phone = request.Phone;
        if (request.Email != null) employee.Email = request.Email;
        if (request.FaceImageUrl != null) employee.FaceImageUrl = request.FaceImageUrl;
        if (request.Status.HasValue) employee.Status = request.Status;

        await _context.SaveChangesAsync();

        // Reload navigation properties
        await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        await _context.Entry(employee).Reference(e => e.Position).LoadAsync();

        return Ok(new EmployeeResponse
        {
            EmployeeId = employee.EmployeeId,
            FullName = employee.FullName,
            Phone = employee.Phone,
            Email = employee.Email,
            FaceImageUrl = employee.FaceImageUrl,
            Status = employee.Status,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name,
            PositionId = employee.PositionId,
            PositionName = employee.Position?.Name
        });
    }

    /// <summary>XÃ³a nhÃ¢n viÃªn (chá»‰ Admin)</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn ID {id}" });

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        // Broadcast real-time update tá»›i clients Ä‘ang theo dÃµi
        int total = await _context.Employees.CountAsync();
        int active = await _context.Employees.CountAsync(e => e.Status == true);
        await _hubContext.Clients.Group("stats").SendAsync("ReceiveStatsUpdate", new EmployeeCountChangedEvent
        {
            TotalEmployees = total,
            ActiveEmployees = active,
            ChangeType = "deleted",
            ChangedAt = DateTime.Now
        });

        return NoContent();
    }

    /// <summary>Upload áº£nh khuÃ´n máº·t nhÃ¢n viÃªn tá»« file mÃ¡y tÃ­nh (chá»‰ Admin)</summary>
    [HttpPost("{id}/face")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFace(int id, IFormFile file)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn ID {id}" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lÃ²ng chá»n file áº£nh" });

        // Kiá»ƒm tra Ä‘á»‹nh dáº¡ng file
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Chá»‰ cháº¥p nháº­n file áº£nh (JPG, PNG, WebP)" });

        // Giá»›i háº¡n dung lÆ°á»£ng 5MB
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "KÃ­ch thÆ°á»›c áº£nh khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 5MB" });

        // Táº¡o thÆ° má»¥c lÆ°u áº£nh náº¿u chÆ°a cÃ³
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "faces");
        Directory.CreateDirectory(uploadFolder);

        // XÃ³a áº£nh cÅ© náº¿u cÃ³
        if (!string.IsNullOrEmpty(employee.FaceImageUrl))
        {
            var oldFileName = Path.GetFileName(employee.FaceImageUrl);
            var oldFilePath = Path.Combine(uploadFolder, oldFileName);
            if (System.IO.File.Exists(oldFilePath))
                System.IO.File.Delete(oldFilePath);
        }

        // Táº¡o tÃªn file duy nháº¥t
        var ext = Path.GetExtension(file.FileName).ToLower();
        var newFileName = $"emp_{id}_{Guid.NewGuid():N}{ext}";
        var newFilePath = Path.Combine(uploadFolder, newFileName);

        // LÆ°u file
        using (var stream = new FileStream(newFilePath, FileMode.Create))
            await file.CopyToAsync(stream);

        // Cáº­p nháº­t URL vÃ o DB (dáº¡ng path tÆ°Æ¡ng Ä‘á»‘i Ä‘á»ƒ serve qua static files)
        employee.FaceImageUrl = $"/uploads/faces/{newFileName}";
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Upload áº£nh thÃ nh cÃ´ng",
            employeeId = id,
            faceImageUrl = employee.FaceImageUrl
        });
    }

    [HttpGet("{id}/face-image")]
    public async Task<IActionResult> GetFaceImage(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

        if (employee == null)
            return NotFound(new { message = $"Khong tim thay nhan vien ID {id}" });

        if (string.IsNullOrWhiteSpace(employee.FaceImageUrl))
            return NotFound(new { message = "Nhan vien chua co anh khuon mat" });

        if (employee.FaceImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            employee.FaceImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Anh ben ngoai khong duoc proxy qua endpoint noi bo" });
        }

        var fileName = Path.GetFileName(employee.FaceImageUrl);
        if (string.IsNullOrWhiteSpace(fileName))
            return NotFound(new { message = "Duong dan anh khong hop le" });

        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "faces");
        var fullPath = Path.GetFullPath(Path.Combine(uploadFolder, fileName));
        var allowedRoot = Path.GetFullPath(uploadFolder);

        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            return NotFound(new { message = "Khong tim thay file anh" });

        await AuditEvidenceRead("EmployeeFaceImage", id.ToString(), fullPath);
        return PhysicalFile(fullPath, ResolveImageContentType(fullPath), enableRangeProcessing: false);
    }

    private async Task AuditEvidenceRead(string entityName, string entityId, string filePath)
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
                fileName = Path.GetFileName(filePath)
            }),
            IsSuccess = true,
            StatusCode = StatusCodes.Status200OK
        });
        await _context.SaveChangesAsync();
    }

    private static string ResolveImageContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}

