using API.Data;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/demo-control")]
[Authorize(Roles = "Admin")]
public class DemoControlController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DemoControlController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        if (!_environment.IsDevelopment() || !_configuration.GetValue("DemoData:Enabled", false))
            return NotFound(new { message = "Tính năng điều khiển demo bị vô hiệu hóa ngoài môi trường demo phát triển." });

        var summary = DemoDataSeeder.ResetOperationalScenarios(_context);
        return Ok(new { message = "Đã đặt lại kịch bản demo thành công.", summary, resetAtUtc = DateTime.UtcNow });
    }
}
