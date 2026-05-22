using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleManagementService _vehicleService;

    public VehiclesController(IVehicleManagementService vehicleManagementService)
    {
        _vehicleService = vehicleManagementService;
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetVehicleTypes()
    {
        var vehicleTypes = await _vehicleService.GetVehicleTypesAsync();
        return Ok(vehicleTypes);
    }

    // GET: api/vehicles
    // Láº¥y danh sÃ¡ch táº¥t cáº£ phÆ°Æ¡ng tiá»‡n
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _vehicleService.GetAllAsync();
        return Ok(vehicles);
    }

    // GET: api/vehicles/5
    // Láº¥y phÆ°Æ¡ng tiá»‡n theo ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y phÆ°Æ¡ng tiá»‡n vá»›i ID = {id}." });

        return Ok(vehicle);
    }

    // GET: api/vehicles/license-plate/51A-12345
    // Tra cá»©u phÆ°Æ¡ng tiá»‡n theo biá»ƒn sá»‘
    [HttpGet("license-plate/{plate}")]
    public async Task<IActionResult> GetByLicensePlate(string plate)
    {
        var vehicle = await _vehicleService.GetByLicensePlateAsync(plate.ToUpper());
        if (vehicle == null)
            return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y phÆ°Æ¡ng tiá»‡n vá»›i biá»ƒn sá»‘ '{plate}'." });

        return Ok(vehicle);
    }

    // GET: api/vehicles/employee/10
    // Láº¥y danh sÃ¡ch phÆ°Æ¡ng tiá»‡n cá»§a má»™t nhÃ¢n viÃªn
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId(int employeeId)
    {
        var vehicles = await _vehicleService.GetByEmployeeIdAsync(employeeId);
        return Ok(vehicles);
    }

    // POST: api/vehicles
    // ÄÄƒng kÃ½ phÆ°Æ¡ng tiá»‡n má»›i
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _vehicleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.VehicleId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // PUT: api/vehicles/5
    // Cáº­p nháº­t thÃ´ng tin phÆ°Æ¡ng tiá»‡n
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _vehicleService.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y phÆ°Æ¡ng tiá»‡n vá»›i ID = {id}." });

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // DELETE: api/vehicles/5
    // XÃ³a Ä‘Äƒng kÃ½ phÆ°Æ¡ng tiá»‡n
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _vehicleService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y phÆ°Æ¡ng tiá»‡n vá»›i ID = {id}." });

        return NoContent();
    }
}

