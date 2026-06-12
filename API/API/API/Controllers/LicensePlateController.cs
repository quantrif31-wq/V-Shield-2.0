using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Data;
using API.Services;

namespace API.Controllers
{
    [Route("api/license-plates")]
    [ApiController]
    [EnableRateLimiting("ops")]
    [Authorize(Roles = "Admin,BaoVe")]
    public class LicensePlateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPlateFuzzyService _plateFuzzy;

        public LicensePlateController(ApplicationDbContext context, IPlateFuzzyService plateFuzzy)
        {
            _context = context;
            _plateFuzzy = plateFuzzy;
        }

        // =========================
        // L?Y DANH SÁCH CAMERA
        // =========================

        [HttpGet("cameras")]
        public async Task<IActionResult> GetCameras()
        {
            var cameras = await _context.CameraPlates
                .Select(x => new
                {
                    CameraIP = x.CameraIP
                })
                .ToListAsync();

            return Ok(cameras);
        }

        // =========================
        // L?Y T?T C? BI?N S? REALTIME
        // =========================

        [HttpGet("plates")]
        public async Task<IActionResult> GetPlates()
        {
            var plates = await _context.CameraPlates
                .OrderByDescending(x => x.LastUpdate)
                .Select(p => new
                {
                    p.CameraIP,
                    p.PlateNumber,
                    p.X1,
                    p.Y1,
                    p.X2,
                    p.Y2,
                    p.LastUpdate
                })
                .ToListAsync();

            return Ok(plates);
        }

        // =========================
        // L?Y BI?N S? THEO CAMERA
        // =========================

        [HttpGet("plate")]
        public async Task<IActionResult> GetPlate(string ip)
        {
            var plate = await _context.CameraPlates
                .FirstOrDefaultAsync(x => x.CameraIP == ip);

            if (plate == null)
                return NotFound();

            return Ok(plate);
        }

        // =========================
        // CAMERA + PLATE (dashboard)
        // =========================

        [HttpGet("camera-plates")]
        public async Task<IActionResult> GetCameraPlates()
        {
            var data = await _context.Cameras
                .Select(c => new
                {
                    c.CameraId,
                    c.CameraName,
                    c.CameraType,

                    Plate = _context.CameraPlates
                        .Where(p => p.CameraIP == c.CameraName)
                        .Select(p => new
                        {
                            p.PlateNumber,
                            p.X1,
                            p.Y1,
                            p.X2,
                            p.Y2,
                            p.LastUpdate
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost("fuzzy-match")]
        public async Task<IActionResult> FuzzyMatch([FromBody] FuzzyMatchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Plate))
                return BadRequest(new { message = "Bien so khong duoc de trong." });

            var results = await _plateFuzzy.FindSimilarPlatesAsync(
                request.Plate, request.MinScore, request.MaxResults);

            return Ok(new
            {
                query = request.Plate,
                normalized = LicensePlateHelper.NormalizeForMatch(request.Plate),
                results
            });
        }

        [HttpGet("{plate}/timeline")]
        public async Task<IActionResult> GetTimeline(string plate, [FromQuery] int hours = 24)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return BadRequest(new { message = "Bien so khong duoc de trong." });

            var entries = await _plateFuzzy.GetPlateTimelineAsync(plate, hours);

            return Ok(new
            {
                plate,
                normalized = LicensePlateHelper.NormalizeForMatch(plate),
                hours,
                entries
            });
        }

        [HttpGet("{plate}/anomalies")]
        public async Task<IActionResult> GetAnomalies(string plate, [FromQuery] int hours = 24)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return BadRequest(new { message = "Bien so khong duoc de trong." });

            var anomalies = await _plateFuzzy.CheckAnomaliesAsync(plate, hours);

            return Ok(new
            {
                plate,
                normalized = LicensePlateHelper.NormalizeForMatch(plate),
                hours,
                anomalies
            });
        }

        [HttpPost("suggest-correction")]
        public async Task<IActionResult> SuggestCorrection([FromBody] SuggestCorrectionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RawOcr))
                return BadRequest(new { message = "Du lieu OCR khong duoc de trong." });

            var result = await _plateFuzzy.SuggestCorrectionAsync(request.RawOcr);

            return Ok(result);
        }
    }

    public class FuzzyMatchRequest
    {
        public string Plate { get; set; } = string.Empty;
        public double MinScore { get; set; } = 0.6;
        public int MaxResults { get; set; } = 10;
    }

    public class SuggestCorrectionRequest
    {
        public string RawOcr { get; set; } = string.Empty;
    }
}





