using API.Data;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/access-permissions")]
[ApiController]
[Authorize]
[RequireOperationalTask("restricted-zone")]
public class AccessPermissionQueryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AccessPermissionQueryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("employee-matrix")]
    public async Task<IActionResult> GetEmployeePermissionMatrix([FromQuery] string? query = null, [FromQuery] int? gateId = null)
    {
        var employeesQuery = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.Status == null || e.Status == true);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            employeesQuery = employeesQuery.Where(e =>
                e.FullName.Contains(normalized) ||
                e.EmployeeId.ToString().Contains(normalized));
        }

        var employees = await employeesQuery
            .OrderBy(e => e.FullName)
            .Select(e => new
            {
                e.EmployeeId,
                e.FullName,
                e.PositionId,
                departmentName = e.Department != null ? e.Department.Name : null,
                positionName = e.Position != null ? e.Position.Name : null
            })
            .ToListAsync();

        var employeeIds = employees.Select(e => e.EmployeeId).ToList();

        // Quyền tường minh theo từng nhân viên (được gạt tay — override)
        var explicitPermissions = await _context.EmployeeAccessPermissions
            .AsNoTracking()
            .Where(p => employeeIds.Contains(p.EmployeeId))
            .Select(p => new { p.EmployeeId, p.GateId, p.IsAllowed })
            .ToListAsync();

        var explicitByEmployee = explicitPermissions
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => group.ToList());

        // Quyền mặc định theo chức vụ (kế thừa ngay khi đọc)
        var positionIds = employees
            .Where(e => e.PositionId.HasValue)
            .Select(e => e.PositionId!.Value)
            .Distinct()
            .ToList();

        var positionPermissions = await _context.PositionAccessPermissions
            .AsNoTracking()
            .Where(p => p.IsAllowed && positionIds.Contains(p.PositionId))
            .Select(p => new { p.PositionId, p.GateId })
            .ToListAsync();

        var positionGatesByPosition = positionPermissions
            .GroupBy(item => item.PositionId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.GateId).ToHashSet());

        var gatesRef = await _context.Gates
            .AsNoTracking()
            .OrderBy(g => g.GateName)
            .Select(g => new
            {
                g.GateId,
                g.GateName,
                g.Location
            })
            .ToListAsync();

        var gateNames = gatesRef.ToDictionary(g => g.GateId, g => g.GateName);

        var mapped = new List<EmployeeMatrixItemResponse>();
        foreach (var employee in employees)
        {
            var explicitOn = new HashSet<int>();
            var explicitOff = new HashSet<int>();
            if (explicitByEmployee.TryGetValue(employee.EmployeeId, out var explicitRows))
            {
                explicitOn = explicitRows.Where(x => x.IsAllowed).Select(x => x.GateId).ToHashSet();
                explicitOff = explicitRows.Where(x => !x.IsAllowed).Select(x => x.GateId).ToHashSet();
            }

            var inherited = new HashSet<int>();
            if (employee.PositionId.HasValue &&
                positionGatesByPosition.TryGetValue(employee.PositionId.Value, out var positionGates))
            {
                inherited = positionGates;
            }

            var allowed = new List<GateItemResponse>();
            foreach (var candidateGateId in explicitOn.Union(inherited))
            {
                // Gạt tắt tay (IsAllowed=false) luôn ghi đè quyền mặc định của chức vụ
                if (explicitOff.Contains(candidateGateId)) continue;
                if (!gateNames.TryGetValue(candidateGateId, out var gateName)) continue;
                allowed.Add(new GateItemResponse
                {
                    GateId = candidateGateId,
                    GateName = gateName,
                    Source = explicitOn.Contains(candidateGateId) ? "manual" : "position"
                });
            }

            var positionGateItems = inherited
                .Where(gateNames.ContainsKey)
                .Select(gateId => new GateItemResponse
                {
                    GateId = gateId,
                    GateName = gateNames[gateId],
                    Source = "position"
                })
                .OrderBy(g => g.GateName)
                .ToList();

            var deniedGateItems = explicitOff
                .Where(gateNames.ContainsKey)
                .Select(gateId => new GateItemResponse
                {
                    GateId = gateId,
                    GateName = gateNames[gateId],
                    Source = "manual"
                })
                .OrderBy(g => g.GateName)
                .ToList();

            mapped.Add(new EmployeeMatrixItemResponse
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                DepartmentName = employee.departmentName,
                PositionName = employee.positionName,
                PositionId = employee.PositionId,
                allowedGates = allowed.OrderBy(g => g.GateName).ToList(),
                positionGates = positionGateItems,
                deniedGates = deniedGateItems
            });
        }

        var filteredEmployees = mapped;
        if (gateId.HasValue)
        {
            filteredEmployees = mapped
                .Where(m => m.allowedGates.Any(g => g.GateId == gateId.Value))
                .ToList();
        }

        return Ok(new
        {
            employees = filteredEmployees,
            gates = gatesRef
        });
    }

    [HttpGet("position-matrix")]
    public async Task<IActionResult> GetPositionPermissionMatrix()
    {
        var positions = await _context.Positions
            .AsNoTracking()
            .OrderBy(p => p.PositionId)
            .Select(p => new
            {
                p.PositionId,
                p.Name,
                EmployeeCount = p.Employees.Count
            })
            .ToListAsync();

        var positionIds = positions.Select(p => p.PositionId).ToList();

        var permissions = await _context.PositionAccessPermissions
            .AsNoTracking()
            .Where(p => p.IsAllowed && positionIds.Contains(p.PositionId))
            .Join(
                _context.Gates.AsNoTracking(),
                permission => permission.GateId,
                gate => gate.GateId,
                (permission, gate) => new
                {
                    permission.PositionId,
                    gate.GateId,
                    gate.GateName
                })
            .ToListAsync();

        var byPosition = permissions
            .GroupBy(item => item.PositionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.GateName)
                    .Select(item => new GateItemResponse
                    {
                        GateId = item.GateId,
                        GateName = item.GateName,
                        Source = "position"
                    })
                    .ToList());

        var mapped = positions.Select(position => new PositionMatrixItemResponse
        {
            PositionId = position.PositionId,
            Name = position.Name,
            EmployeeCount = position.EmployeeCount,
            allowedGates = byPosition.TryGetValue(position.PositionId, out var gates)
                ? gates
                : new List<GateItemResponse>()
        }).ToList();

        var gatesRef = await _context.Gates
            .AsNoTracking()
            .OrderBy(g => g.GateName)
            .Select(g => new
            {
                g.GateId,
                g.GateName,
                g.Location
            })
            .ToListAsync();

        return Ok(new
        {
            positions = mapped,
            gates = gatesRef
        });
    }

    [HttpDelete("employee/{employeeId:int}/gate/{gateId:int}")]
    public async Task<IActionResult> DeleteEmployeePermission(int employeeId, int gateId)
    {
        var permission = await _context.EmployeeAccessPermissions
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.GateId == gateId);

        if (permission == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Khong tim thay quyen de xoa."
            });
        }

        _context.EmployeeAccessPermissions.Remove(permission);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Da xoa quyen truy cap."
        });
    }

    [HttpGet("visitor-matrix")]
    public async Task<IActionResult> GetVisitorPermissionMatrix([FromQuery] string? query = null, [FromQuery] int? gateId = null)
    {
        var visitorsQuery = _context.VisitorDetails
            .AsNoTracking()
            .Include(v => v.Registration)
            .Where(v => v.IsQrActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            visitorsQuery = visitorsQuery.Where(v =>
                v.FullName.Contains(normalized) ||
                v.VisitorDetailId.ToString().Contains(normalized));
        }

        var visitors = await visitorsQuery
            .OrderBy(v => v.FullName)
            .Select(v => new
            {
                v.VisitorDetailId,
                v.FullName,
                registrationId = v.RegistrationId,
                registrationStatus = v.Registration != null ? v.Registration.Status : null
            })
            .ToListAsync();

        var visitorIds = visitors.Select(v => v.VisitorDetailId).ToList();

        var permissionsQuery = _context.VisitorAccessPermissions
            .AsNoTracking()
            .Where(p => p.IsAllowed && visitorIds.Contains(p.VisitorDetailId));

        if (gateId.HasValue)
        {
            permissionsQuery = permissionsQuery.Where(p => p.GateId == gateId.Value);
        }

        var grantedPermissions = await permissionsQuery
            .Join(
                _context.Gates.AsNoTracking(),
                permission => permission.GateId,
                gate => gate.GateId,
                (permission, gate) => new
                {
                    permission.VisitorDetailId,
                    gate.GateId,
                    gate.GateName
                })
            .ToListAsync();

        var permissionsByVisitor = grantedPermissions
            .GroupBy(item => item.VisitorDetailId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.GateName)
                    .Select(item => new GateItemResponse
                    {
                        GateId = item.GateId,
                        GateName = item.GateName
                    })
                    .ToList());

        var filteredVisitors = visitors;
        if (gateId.HasValue)
        {
            filteredVisitors = visitors
                .Where(visitor => permissionsByVisitor.ContainsKey(visitor.VisitorDetailId))
                .ToList();
        }

        var mapped = filteredVisitors.Select(visitor => new VisitorMatrixItemResponse
        {
            VisitorDetailId = visitor.VisitorDetailId,
            FullName = visitor.FullName,
            RegistrationId = visitor.registrationId,
            RegistrationStatus = visitor.registrationStatus,
            allowedGates = permissionsByVisitor.TryGetValue(visitor.VisitorDetailId, out var gates)
                ? gates
                : new List<GateItemResponse>()
        });

        var gatesRef = await _context.Gates
            .AsNoTracking()
            .OrderBy(g => g.GateName)
            .Select(g => new
            {
                g.GateId,
                g.GateName,
                g.Location
            })
            .ToListAsync();

        return Ok(new
        {
            visitors = mapped,
            gates = gatesRef
        });
    }

    [HttpDelete("visitor/{visitorDetailId:int}/gate/{gateId:int}")]
    public async Task<IActionResult> DeleteVisitorPermission(int visitorDetailId, int gateId)
    {
        var permission = await _context.VisitorAccessPermissions
            .FirstOrDefaultAsync(p => p.VisitorDetailId == visitorDetailId && p.GateId == gateId);

        if (permission == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Khong tim thay quyen de xoa."
            });
        }

        _context.VisitorAccessPermissions.Remove(permission);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Da xoa quyen truy cap."
        });
    }
}

public sealed class EmployeeMatrixItemResponse
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public int? PositionId { get; set; }
    public List<GateItemResponse> allowedGates { get; set; } = new();
    public List<GateItemResponse> positionGates { get; set; } = new();
    public List<GateItemResponse> deniedGates { get; set; } = new();
}

public sealed class GateItemResponse
{
    public int GateId { get; set; }
    public string GateName { get; set; } = string.Empty;
    public string Source { get; set; } = "manual";
}

public sealed class PositionMatrixItemResponse
{
    public int PositionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public List<GateItemResponse> allowedGates { get; set; } = new();
}

public sealed class VisitorMatrixItemResponse
{
    public int VisitorDetailId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int RegistrationId { get; set; }
    public string? RegistrationStatus { get; set; }
    public List<GateItemResponse> allowedGates { get; set; } = new();
}
