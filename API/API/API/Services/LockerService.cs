using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class LockerService
{
    private readonly ApplicationDbContext _context;

    public LockerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LockerCompartment>> GetAvailableCompartmentsAsync(int cabinetId)
    {
        return await _context.LockerCompartments
            .Where(c => c.LockerCabinetId == cabinetId && c.Status == "Empty")
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<List<LockerCompartment>> GetOccupiedCompartmentsAsync(int cabinetId)
    {
        return await _context.LockerCompartments
            .Where(c => c.LockerCabinetId == cabinetId && c.Status == "Occupied")
            .Include(c => c.EvidenceItem)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> AssignCompartmentAsync(int compartmentId, long evidenceItemId, int userId)
    {
        var compartment = await _context.LockerCompartments
            .Include(c => c.EvidenceItem)
            .FirstOrDefaultAsync(c => c.LockerCompartmentId == compartmentId);

        if (compartment == null)
            return (false, "Compartment not found.");
        if (compartment.Status != "Empty")
            return (false, $"Compartment is {compartment.Status}.");

        var evidenceItem = await _context.EvidenceItems.FindAsync(evidenceItemId);
        if (evidenceItem == null)
            return (false, "Evidence item not found.");

        compartment.Status = "Occupied";
        compartment.EvidenceItemId = evidenceItemId;
        compartment.OccupiedByUserId = userId;
        compartment.OccupiedAtUtc = DateTime.UtcNow;
        compartment.ReleasedAtUtc = null;

        _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
        {
            EvidenceItemId = evidenceItemId,
            Action = "StoredInLocker",
            ActorUserId = userId,
            ToCustodian = $"{compartment.Code}",
            Note = $"Stored in locker compartment {compartment.Code}"
        });

        _context.LockerAccessLogs.Add(new LockerAccessLog
        {
            LockerCompartmentId = compartmentId,
            UserId = userId,
            Action = "Assigned",
            Purpose = $"Evidence #{evidenceItemId} stored"
        });

        await _context.SaveChangesAsync();
        return (true, $"Assigned to compartment {compartment.Code}");
    }

    public async Task<(bool Success, string Message)> AssignCompartmentToFoundItemAsync(
        int compartmentId,
        long foundItemReportId,
        long? evidenceItemId,
        int userId)
    {
        var compartment = await _context.LockerCompartments
            .Include(c => c.EvidenceItem)
            .FirstOrDefaultAsync(c => c.LockerCompartmentId == compartmentId);

        if (compartment == null)
            return (false, "Compartment not found.");
        if (compartment.Status != "Empty")
            return (false, $"Compartment is {compartment.Status}.");

        compartment.Status = "Occupied";
        compartment.EvidenceItemId = evidenceItemId;
        compartment.OccupiedByUserId = userId;
        compartment.OccupiedAtUtc = DateTime.UtcNow;
        compartment.ReleasedAtUtc = null;

        if (evidenceItemId.HasValue)
        {
            _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
            {
                EvidenceItemId = evidenceItemId.Value,
                Action = "StoredInLocker",
                ActorUserId = userId,
                ToCustodian = $"{compartment.Code}",
                Note = $"Found item #{foundItemReportId} stored in locker compartment {compartment.Code}"
            });
        }

        _context.LockerAccessLogs.Add(new LockerAccessLog
        {
            LockerCompartmentId = compartmentId,
            UserId = userId,
            Action = "AssignedFoundItem",
            Purpose = $"Found item #{foundItemReportId} stored"
        });

        await _context.SaveChangesAsync();
        return (true, $"Assigned found item to compartment {compartment.Code}");
    }

    public async Task<(bool Success, string Message)> ReleaseCompartmentAsync(int compartmentId, int userId)
    {
        var compartment = await _context.LockerCompartments
            .Include(c => c.EvidenceItem)
            .FirstOrDefaultAsync(c => c.LockerCompartmentId == compartmentId);

        if (compartment == null)
            return (false, "Compartment not found.");
        if (compartment.Status != "Occupied")
            return (false, $"Compartment is {compartment.Status}.");

        var evidenceItemId = compartment.EvidenceItemId;

        compartment.Status = "Empty";
        compartment.EvidenceItemId = null;
        compartment.OccupiedByUserId = null;
        compartment.ReleasedAtUtc = DateTime.UtcNow;

        if (evidenceItemId.HasValue)
        {
            _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
            {
                EvidenceItemId = evidenceItemId.Value,
                Action = "ReleasedFromLocker",
                ActorUserId = userId,
                FromCustodian = $"{compartment.Code}",
                Note = $"Released from locker compartment {compartment.Code}"
            });
        }

        _context.LockerAccessLogs.Add(new LockerAccessLog
        {
            LockerCompartmentId = compartmentId,
            UserId = userId,
            Action = "Released",
            Purpose = $"Evidence #{evidenceItemId} retrieved"
        });

        await _context.SaveChangesAsync();
        return (true, $"Released compartment {compartment.Code}");
    }

    public async Task<List<LockerAccessLog>> GetAccessLogsAsync(int? compartmentId = null, int limit = 100)
    {
        var query = _context.LockerAccessLogs
            .Include(l => l.Compartment)
            .AsNoTracking()
            .AsQueryable();

        if (compartmentId.HasValue)
            query = query.Where(l => l.LockerCompartmentId == compartmentId.Value);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
