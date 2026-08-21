using System.Globalization;
using System.Text;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Agent;

/// <summary>
/// Tự sinh email công ty theo domain VPS cho từng người (nhân viên + khách),
/// thiết kế KHÔNG TRÙNG: dùng slug tên + hậu tố số khi xung đột.
/// Cấu hình domain 1 lần (Mail:Domain) — không ai phải đăng nhập mail cá nhân.
/// </summary>
public sealed class CompanyEmailService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CompanyEmailService> _logger;

    public CompanyEmailService(ApplicationDbContext db, ILogger<CompanyEmailService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public static string NormalizeToSlug(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";
        var s = fullName.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            var c = char.ToLowerInvariant(ch);
            if (c == 'đ') c = 'd';
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
            else if (c == ' ' || c == '.' || c == '-') sb.Append('.');
        }
        var tokens = sb.ToString()
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .ToArray();
        return string.Join(".", tokens);
    }

    /// <summary>Sinh email duy nhất dạng slug@domain, thêm hậu tố số nếu trùng.</summary>
    public async Task<string> GenerateUniqueAsync(string fullName, string fallbackKey, string domain, CancellationToken ct)
    {
        var cleanDomain = (domain ?? "").Trim().TrimStart('@').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleanDomain))
            throw new InvalidOperationException("Chưa cấu hình Mail:Domain.");

        var baseSlug = NormalizeToSlug(fullName);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = NormalizeToSlug(fallbackKey);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "nguoidung";
        baseSlug = baseSlug.Length > 48 ? baseSlug[..48] : baseSlug;

        var candidate = $"{baseSlug}@{cleanDomain}";
        if (!await ExistsAsync(candidate, ct)) return candidate;

        for (int i = 2; i < 1000; i++)
        {
            var c2 = $"{baseSlug}.{i}@{cleanDomain}";
            if (!await ExistsAsync(c2, ct)) return c2;
        }

        // fallback: dùng key (vd mã NV / id) để đảm bảo unique tuyệt đối
        var keySlug = NormalizeToSlug(fallbackKey);
        if (string.IsNullOrWhiteSpace(keySlug)) keySlug = Guid.NewGuid().ToString("N")[..8];
        return $"{baseSlug}.{keySlug}@{cleanDomain}";
    }

    private async Task<bool> ExistsAsync(string email, CancellationToken ct)
    {
        var norm = email.ToLowerInvariant();
        return await _db.Employees.AnyAsync(e => e.CompanyEmail != null && e.CompanyEmail.ToLower() == norm, ct)
            || await _db.Employees.AnyAsync(e => e.Email != null && e.Email.ToLower() == norm, ct)
            || await _db.VisitorDetails.AnyAsync(v => v.CompanyEmail != null && v.CompanyEmail.ToLower() == norm, ct);
    }

    /// <summary>Điền CompanyEmail còn trống cho toàn bộ nhân viên + khách.</summary>
    public async Task<int> EnsureBackfillAsync(string domain, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(domain)) return 0;
        int updated = 0;

        var employees = await _db.Employees
            .Where(e => e.CompanyEmail == null)
            .OrderBy(e => e.EmployeeId)
            .ToListAsync(ct);
        foreach (var e in employees)
        {
            e.CompanyEmail = await GenerateUniqueAsync(e.FullName, e.EmployeeCode ?? $"nv{e.EmployeeId}", domain, ct);
            updated++;
        }

        var visitors = await _db.VisitorDetails
            .Where(v => v.CompanyEmail == null)
            .OrderBy(v => v.VisitorDetailId)
            .ToListAsync(ct);
        foreach (var v in visitors)
        {
            v.CompanyEmail = await GenerateUniqueAsync(v.FullName, $"khach{v.VisitorDetailId}", domain, ct);
            updated++;
        }

        if (updated > 0) await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Backfill company email: {Count} bản ghi, domain={Domain}", updated, domain);
        return updated;
    }
}