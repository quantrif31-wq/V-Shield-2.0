using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/system-config")]
[Authorize]
public class SystemConfigController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SystemConfigController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var entry = await _context.SystemConfigs.FindAsync(key);
        return Ok(new { key, value = entry?.Value ?? "" });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Upsert(string key, [FromBody] SystemConfigUpsertRequest request)
    {
        var entry = await _context.SystemConfigs.FindAsync(key);
        if (entry == null)
        {
            entry = new SystemConfig { Key = key, Value = request.Value };
            _context.SystemConfigs.Add(entry);
        }
        else
        {
            entry.Value = request.Value;
        }
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { key, value = entry.Value });
    }
}

public class SystemConfigUpsertRequest
{
    public string Value { get; set; } = string.Empty;
}
