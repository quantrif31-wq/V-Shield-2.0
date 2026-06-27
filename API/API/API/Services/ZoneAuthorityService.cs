using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class ZoneAuthorityService
{
    private readonly ApplicationDbContext _db;

    public ZoneAuthorityService(ApplicationDbContext db)
    {
        _db = db;
    }

    public static readonly string[] AuthorityLevelOrder = ["Normal", "Elevated", "Full"];

    public static readonly string[] SecurityLevelOrder = ["Public", "Normal", "Restricted", "Critical"];

    public static int GetLevelIndex(string level, string[] order)
    {
        var idx = Array.IndexOf(order, level);
        return idx >= 0 ? idx : -1;
    }

    public static string RequiredAuthorityForZone(string securityLevel)
    {
        return securityLevel switch
        {
            "Critical" => "Full",
            "Restricted" => "Elevated",
            _ => "Normal"
        };
    }

    /// <summary>Kiểm tra user có quyền override ở zone cụ thể không</summary>
    public async Task<(bool Allowed, string Reason)> CanOverrideZoneAsync(int userId, int securityZoneId)
    {
        var zone = await _db.SecurityZones.FindAsync(securityZoneId);
        if (zone == null)
            return (false, "Khu vực không tồn tại.");

        if (!zone.IsActive)
            return (false, "Khu vực đã ngừng hoạt động.");

        var now = DateTime.UtcNow;
        var authority = await _db.GuardZoneAuthorities
            .Where(a => a.UserId == userId
                     && a.SecurityZoneId == securityZoneId
                     && a.CanOverride
                     && a.ValidFrom <= now
                     && a.ValidTo >= now)
            .OrderByDescending(a => GetLevelIndex(a.AuthorityLevel, AuthorityLevelOrder))
            .FirstOrDefaultAsync();

        if (authority == null)
            return (false, "Bạn không được phân quyền override tại khu vực này.");

        var required = RequiredAuthorityForZone(zone.SecurityLevel);
        if (GetLevelIndex(authority.AuthorityLevel, AuthorityLevelOrder) < GetLevelIndex(required, AuthorityLevelOrder))
            return (false, $"Khu vực {zone.Name} yêu cầu cấp độ '{required}' để override. Cấp hiện tại: '{authority.AuthorityLevel}'.");

        return (true, "OK");
    }

    /// <summary>Kiểm tra user có quyền override tại site nào đó không (kiểm tra zone đầu tiên tìm thấy)</summary>
    public async Task<(bool Allowed, string Reason)> CanOverrideAnyZoneAtSiteAsync(int userId, int siteId)
    {
        var zoneIds = await _db.SecurityZones
            .Where(z => z.SiteId == siteId && z.IsActive)
            .Select(z => z.SecurityZoneId)
            .ToListAsync();

        if (zoneIds.Count == 0)
            return (false, "Site không có khu vực nào.");

        foreach (var zoneId in zoneIds)
        {
            var result = await CanOverrideZoneAsync(userId, zoneId);
            if (result.Allowed)
                return (true, "OK");
        }

        return (false, "Bạn không có quyền override tại bất kỳ khu vực nào thuộc site này.");
    }

    /// <summary>Lấy tất cả zone mà user có quyền</summary>
    public async Task<List<SecurityZone>> GetAuthorizedZonesAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var zoneIds = await _db.GuardZoneAuthorities
            .Where(a => a.UserId == userId
                     && a.CanOverride
                     && a.ValidFrom <= now
                     && a.ValidTo >= now)
            .Select(a => a.SecurityZoneId)
            .Distinct()
            .ToListAsync();

        return await _db.SecurityZones
            .Where(z => zoneIds.Contains(z.SecurityZoneId) && z.IsActive)
            .Include(z => z.Site)
            .ToListAsync();
    }
}
