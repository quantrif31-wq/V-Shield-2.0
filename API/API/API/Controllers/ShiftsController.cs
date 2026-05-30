using API.Data;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;

    public ShiftsController(ApplicationDbContext context, IAttendancePermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var query = _context.Shifts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.ShiftName.Contains(search));

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var data = await query
            .OrderBy(x => x.StartTime)
            .Select(x => new
            {
                x.ShiftId,
                x.ShiftName,
                x.StartTime,
                x.EndTime,
                x.BreakMinutes,
                x.AllowedLateMinutes,
                x.AllowedEarlyLeaveMinutes,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShiftId == id);

        if (shift == null)
            return NotFound(new { message = $"Khong tim thay ca lam ID {id}" });

        return Ok(shift);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShiftUpsertRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await EnsureCanManageAsync()) return Forbid();
        if (request.StartTime == null || request.EndTime == null)
            return BadRequest(new { message = "StartTime va EndTime la bat buoc." });

        var normalizedName = request.ShiftName.Trim().ToUpperInvariant();
        if (await _context.Shifts.AnyAsync(x => x.ShiftName.ToUpper() == normalizedName))
            return Conflict(new { message = $"Ca lam '{request.ShiftName}' da ton tai." });

        var shift = new Shift
        {
            ShiftName = request.ShiftName.Trim(),
            StartTime = request.StartTime.Value,
            EndTime = request.EndTime.Value,
            BreakMinutes = Math.Max(0, request.BreakMinutes),
            AllowedLateMinutes = Math.Max(0, request.AllowedLateMinutes),
            AllowedEarlyLeaveMinutes = Math.Max(0, request.AllowedEarlyLeaveMinutes),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = shift.ShiftId }, shift);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ShiftUpsertRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await EnsureCanManageAsync()) return Forbid();
        if (request.StartTime == null || request.EndTime == null)
            return BadRequest(new { message = "StartTime va EndTime la bat buoc." });

        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.ShiftId == id);
        if (shift == null)
            return NotFound(new { message = $"Khong tim thay ca lam ID {id}" });

        var normalizedName = request.ShiftName.Trim().ToUpperInvariant();
        if (await _context.Shifts.AnyAsync(x => x.ShiftId != id && x.ShiftName.ToUpper() == normalizedName))
            return Conflict(new { message = $"Ca lam '{request.ShiftName}' da ton tai." });

        shift.ShiftName = request.ShiftName.Trim();
        shift.StartTime = request.StartTime.Value;
        shift.EndTime = request.EndTime.Value;
        shift.BreakMinutes = Math.Max(0, request.BreakMinutes);
        shift.AllowedLateMinutes = Math.Max(0, request.AllowedLateMinutes);
        shift.AllowedEarlyLeaveMinutes = Math.Max(0, request.AllowedEarlyLeaveMinutes);
        shift.IsActive = request.IsActive;
        shift.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(shift);
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (!await EnsureCanManageAsync()) return Forbid();

        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.ShiftId == id);
        if (shift == null)
            return NotFound(new { message = $"Khong tim thay ca lam ID {id}" });

        shift.IsActive = false;
        shift.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Da khoa ca lam.", shiftId = id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        return await Deactivate(id);
    }

    private async Task<bool> EnsureCanManageAsync() =>
        _permissionService.IsAdmin(User) || await _permissionService.IsManagerAsync(User);
}

