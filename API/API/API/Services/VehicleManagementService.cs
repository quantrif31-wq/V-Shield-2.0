using API.DTOs;
using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

// ==================== INTERFACE ====================
public interface IVehicleManagementService
{
    Task<IEnumerable<VehicleTypeDto>> GetVehicleTypesAsync();
    Task<IEnumerable<VehicleDto>> GetAllAsync();
    Task<VehicleDto?> GetByIdAsync(int vehicleId);
    Task<IEnumerable<VehicleDto>> GetByEmployeeIdAsync(int employeeId);
    Task<VehicleDto?> GetByLicensePlateAsync(string licensePlate);
    Task<VehicleDto> CreateAsync(CreateVehicleDto dto);
    Task<VehicleDto?> UpdateAsync(int vehicleId, UpdateVehicleDto dto);
    Task<bool> DeleteAsync(int vehicleId);
}

// ==================== IMPLEMENTATION ====================
public class VehicleManagementService : IVehicleManagementService
{
    private readonly ApplicationDbContext _context;

    public VehicleManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VehicleTypeDto>> GetVehicleTypesAsync()
    {
        await EnsureDefaultVehicleTypesAsync();

        return await _context.VehicleTypes
            .OrderBy(vt => vt.TypeName)
            .Select(vt => new VehicleTypeDto
            {
                VehicleTypeId = vt.VehicleTypeId,
                TypeName = vt.TypeName
            })
            .ToListAsync();
    }


    // Láº¥y táº¥t cáº£ phÆ°Æ¡ng tiá»‡n
    public async Task<IEnumerable<VehicleDto>> GetAllAsync()
    {
        return await _context.Vehicles
            .Include(v => v.Employee)
            .Include(v => v.VehicleType)
            .Select(v => MapToDto(v))
            .ToListAsync();
    }

    // Láº¥y phÆ°Æ¡ng tiá»‡n theo ID
    public async Task<VehicleDto?> GetByIdAsync(int vehicleId)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Employee)
            .Include(v => v.VehicleType)
            .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

        return vehicle == null ? null : MapToDto(vehicle);
    }

    // Láº¥y phÆ°Æ¡ng tiá»‡n theo mÃ£ nhÃ¢n viÃªn
    public async Task<IEnumerable<VehicleDto>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Vehicles
            .Include(v => v.Employee)
            .Include(v => v.VehicleType)
            .Where(v => v.EmployeeId == employeeId)
            .Select(v => MapToDto(v))
            .ToListAsync();
    }

    // Láº¥y phÆ°Æ¡ng tiá»‡n theo biá»ƒn sá»‘
    public async Task<VehicleDto?> GetByLicensePlateAsync(string licensePlate)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Employee)
            .Include(v => v.VehicleType)
            .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate);

        return vehicle == null ? null : MapToDto(vehicle);
    }

    // Táº¡o má»›i phÆ°Æ¡ng tiá»‡n
    public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
    {
        await EnsureDefaultVehicleTypesAsync();

        // Kiá»ƒm tra biá»ƒn sá»‘ Ä‘Ã£ tá»“n táº¡i chÆ°a
        var existing = await _context.Vehicles
            .AnyAsync(v => v.LicensePlate == dto.LicensePlate);
        if (existing)
            throw new InvalidOperationException($"Biá»ƒn sá»‘ '{dto.LicensePlate}' Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½.");

        // Kiá»ƒm tra nhÃ¢n viÃªn tá»“n táº¡i
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.EmployeeId == dto.EmployeeId);
        if (!employeeExists)
            throw new KeyNotFoundException($"KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn vá»›i ID = {dto.EmployeeId}.");

        // Kiá»ƒm tra loáº¡i xe tá»“n táº¡i
        if (dto.VehicleTypeId.HasValue)
        {
            var vehicleTypeExists = await _context.VehicleTypes
                .AnyAsync(vt => vt.VehicleTypeId == dto.VehicleTypeId.Value);
            if (!vehicleTypeExists)
                throw new KeyNotFoundException($"KhÃ´ng tÃ¬m tháº¥y loáº¡i xe vá»›i ID = {dto.VehicleTypeId}. Vui lÃ²ng kiá»ƒm tra báº£ng VehicleType trong database.");
        }

        var vehicle = new Vehicle
        {
            LicensePlate = dto.LicensePlate.Trim().ToUpper(),
            VehicleTypeId = dto.VehicleTypeId,
            EmployeeId = dto.EmployeeId,
            Description = dto.Description
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        // Reload vá»›i navigation properties
        await _context.Entry(vehicle).Reference(v => v.Employee).LoadAsync();
        await _context.Entry(vehicle).Reference(v => v.VehicleType).LoadAsync();

        return MapToDto(vehicle);
    }

    // Cáº­p nháº­t phÆ°Æ¡ng tiá»‡n
    public async Task<VehicleDto?> UpdateAsync(int vehicleId, UpdateVehicleDto dto)
    {
        await EnsureDefaultVehicleTypesAsync();

        var vehicle = await _context.Vehicles
            .Include(v => v.Employee)
            .Include(v => v.VehicleType)
            .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

        if (vehicle == null) return null;

        // Kiá»ƒm tra biá»ƒn sá»‘ má»›i khÃ´ng trÃ¹ng vá»›i xe khÃ¡c
        if (!string.IsNullOrWhiteSpace(dto.LicensePlate))
        {
            var duplicatePlate = await _context.Vehicles
                .AnyAsync(v => v.LicensePlate == dto.LicensePlate && v.VehicleId != vehicleId);
            if (duplicatePlate)
                throw new InvalidOperationException($"Biá»ƒn sá»‘ '{dto.LicensePlate}' Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½ cho xe khÃ¡c.");

            vehicle.LicensePlate = dto.LicensePlate.Trim().ToUpper();
        }

        // Kiá»ƒm tra nhÃ¢n viÃªn má»›i tá»“n táº¡i
        if (dto.EmployeeId.HasValue)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeId == dto.EmployeeId.Value);
            if (!employeeExists)
                throw new KeyNotFoundException($"KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn vá»›i ID = {dto.EmployeeId}.");

            vehicle.EmployeeId = dto.EmployeeId;
        }

        // Kiá»ƒm tra loáº¡i xe má»›i tá»“n táº¡i
        if (dto.VehicleTypeId.HasValue)
        {
            var vehicleTypeExists = await _context.VehicleTypes
                .AnyAsync(vt => vt.VehicleTypeId == dto.VehicleTypeId.Value);
            if (!vehicleTypeExists)
                throw new KeyNotFoundException($"KhÃ´ng tÃ¬m tháº¥y loáº¡i xe vá»›i ID = {dto.VehicleTypeId}. Vui lÃ²ng kiá»ƒm tra báº£ng VehicleType trong database.");

            vehicle.VehicleTypeId = dto.VehicleTypeId;
        }

        if (dto.Description != null)
            vehicle.Description = dto.Description;

        await _context.SaveChangesAsync();

        // Reload navigation properties sau khi update
        await _context.Entry(vehicle).Reference(v => v.Employee).LoadAsync();
        await _context.Entry(vehicle).Reference(v => v.VehicleType).LoadAsync();

        return MapToDto(vehicle);
    }

    // XÃ³a phÆ°Æ¡ng tiá»‡n
    public async Task<bool> DeleteAsync(int vehicleId)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        if (vehicle == null) return false;

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task EnsureDefaultVehicleTypesAsync()
    {
        var defaults = new[] { "Ã” tÃ´", "Xe mÃ¡y", "Xe Ä‘áº¡p", "Xe táº£i" };
        var existingNames = await _context.VehicleTypes
            .Select(vt => vt.TypeName)
            .ToListAsync();

        var missing = defaults
            .Where(name => !existingNames.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => new VehicleType { TypeName = name })
            .ToList();

        if (!missing.Any())
        {
            return;
        }

        _context.VehicleTypes.AddRange(missing);
        await _context.SaveChangesAsync();
    }

    // Helper: Map Entity -> DTO
    private static VehicleDto MapToDto(Vehicle v) => new VehicleDto
    {
        VehicleId = v.VehicleId,
        LicensePlate = v.LicensePlate,
        VehicleTypeId = v.VehicleTypeId,
        VehicleTypeName = v.VehicleType?.TypeName,
        EmployeeId = v.EmployeeId,
        EmployeeFullName = v.Employee?.FullName,
        Description = v.Description
    };
}

