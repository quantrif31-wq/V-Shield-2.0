using API.Data;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/access-permissions")]
[ApiController]
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
                departmentName = e.Department != null ? e.Department.Name : null,
                positionName = e.Position != null ? e.Position.Name : null
            })
            .ToListAsync();

        var employeeIds = employees.Select(e => e.EmployeeId).ToList();

        var permissionsQuery = _context.EmployeeAccessPermissions
            .AsNoTracking()
            .Where(p => p.IsAllowed && employeeIds.Contains(p.EmployeeId));

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
                    permission.EmployeeId,
                    gate.GateId,
                    gate.GateName
                })
            .ToListAsync();

        var permissionsByEmployee = grantedPermissions
            .GroupBy(item => item.EmployeeId)
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

        var filteredEmployees = employees;
        if (gateId.HasValue)
        {
            filteredEmployees = employees
                .Where(employee => permissionsByEmployee.ContainsKey(employee.EmployeeId))
                .ToList();
        }

        var mapped = filteredEmployees.Select(employee => new EmployeeMatrixItemResponse
        {
            EmployeeId = employee.EmployeeId,
            FullName = employee.FullName,
            DepartmentName = employee.departmentName,
            PositionName = employee.positionName,
            allowedGates = permissionsByEmployee.TryGetValue(employee.EmployeeId, out var gates)
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
            employees = mapped,
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
    public List<GateItemResponse> allowedGates { get; set; } = new();
}

public sealed class GateItemResponse
{
    public int GateId { get; set; }
    public string GateName { get; set; } = string.Empty;
}

public sealed class VisitorMatrixItemResponse
{
    public int VisitorDetailId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int RegistrationId { get; set; }
    public string? RegistrationStatus { get; set; }
    public List<GateItemResponse> allowedGates { get; set; } = new();
}
