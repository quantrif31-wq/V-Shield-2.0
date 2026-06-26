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
            return NotFound(new { message = "Demo control is disabled outside the development demo environment." });

        var summary = DemoDataSeeder.ResetOperationalScenarios(_context);
        return Ok(new { message = "Demo scenarios reset successfully.", summary, resetAtUtc = DateTime.UtcNow });
    }
}
