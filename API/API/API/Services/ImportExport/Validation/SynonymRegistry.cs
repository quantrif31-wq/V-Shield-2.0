using System.Text.Json;

namespace API.Services.ImportExport.Validation;

public class SynonymRegistry
{
    private readonly Dictionary<string, string> _synonymMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _reverseMap = new(StringComparer.OrdinalIgnoreCase);

    public SynonymRegistry()
    {
        LoadDefaults();
    }

    public string? FindStandard(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();

        if (_synonymMap.TryGetValue(trimmed, out var standard))
            return standard;

        var normalized = NormalizeForMatch(trimmed);
        foreach (var (key, val) in _synonymMap)
        {
            if (NormalizeForMatch(key) == normalized)
                return val;
        }

        if (double.TryParse(trimmed, out _))
        {
            if (trimmed is "1") return "true";
            if (trimmed is "0") return "false";
        }

        return null;
    }

    public List<string> GetSynonyms(string standardValue)
    {
        return _reverseMap.TryGetValue(standardValue, out var synonyms)
            ? synonyms
            : [standardValue];
    }

    public bool IsStandardValue(string value, string standard)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (string.Equals(value.Trim(), standard, StringComparison.OrdinalIgnoreCase))
            return true;
        var found = FindStandard(value);
        return found != null && string.Equals(found, standard, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForMatch(string s)
    {
        return s.Trim()
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("_", "")
            .Replace("-", "")
            .Replace("/", "");
    }

    private void AddSynonym(string synonym, string standard)
    {
        _synonymMap[synonym] = standard;
        if (!_reverseMap.ContainsKey(standard))
            _reverseMap[standard] = [];
        if (!_reverseMap[standard].Contains(synonym))
            _reverseMap[standard].Add(synonym);
    }

    private void LoadDefaults()
    {
        // Boolean
        AddSynonym("true", "true"); AddSynonym("TRUE", "true"); AddSynonym("True", "true");
        AddSynonym("1", "true"); AddSynonym("yes", "true"); AddSynonym("YES", "true");
        AddSynonym("có", "true"); AddSynonym("co", "true"); AddSynonym("x", "true");
        AddSynonym("✓", "true"); AddSynonym("✔", "true"); AddSynonym("active", "true");
        AddSynonym("false", "false"); AddSynonym("FALSE", "false"); AddSynonym("False", "false");
        AddSynonym("0", "false"); AddSynonym("no", "false"); AddSynonym("NO", "false");
        AddSynonym("không", "false"); AddSynonym("khong", "false"); AddSynonym("inactive", "false");
        AddSynonym("", "false");

        // Roles
        AddSynonym("admin", "Admin"); AddSynonym("ADMIN", "Admin"); AddSynonym("quản trị", "Admin");
        AddSynonym("quản trị viên", "Admin"); AddSynonym("quantri", "Admin"); AddSynonym("quantrivien", "Admin");
        AddSynonym("staff", "Staff"); AddSynonym("STAFF", "Staff"); AddSynonym("nhân viên", "Staff");
        AddSynonym("nhan vien", "Staff"); AddSynonym("nhanvien", "Staff"); AddSynonym("nv", "Staff");
        AddSynonym("employee", "Staff");
        AddSynonym("quản lý", "QuanLy"); AddSynonym("quan ly", "QuanLy"); AddSynonym("manager", "QuanLy");
        AddSynonym("baove", "BaoVe"); AddSynonym("bảo vệ", "BaoVe"); AddSynonym("BẢO VỆ", "BaoVe");
        AddSynonym("bảo vệ", "BaoVe"); AddSynonym("security", "BaoVe"); AddSynonym("guard", "BaoVe");

        // Employee lifecycle status
        AddSynonym("active", "Active"); AddSynonym("ACTIVE", "Active"); AddSynonym("hoạt động", "Active");
        AddSynonym("dang lam viec", "Active"); AddSynonym("đang làm việc", "Active"); AddSynonym("đang hoạt động", "Active");
        AddSynonym("inactive", "Inactive"); AddSynonym("INACTIVE", "Inactive"); AddSynonym("ngừng hoạt động", "Inactive");
        AddSynonym("ngung hđ", "Inactive"); AddSynonym("ngung hoat dong", "Inactive");
        AddSynonym("suspended", "Suspended"); AddSynonym("tạm ngừng", "Suspended");
        AddSynonym("terminated", "Terminated"); AddSynonym("đã nghỉ", "Terminated"); AddSynonym("nghi viec", "Terminated");
        AddSynonym("resigned", "Terminated");

        // Gender
        AddSynonym("male", "Male"); AddSynonym("MALE", "Male"); AddSynonym("nam", "Male");
        AddSynonym("nữ", "Female"); AddSynonym("nu", "Female"); AddSynonym("female", "Female");

        // Common abbreviations
        AddSynonym("stt", "No"); AddSynonym("số thứ tự", "No"); AddSynonym("so thu tu", "No");
        AddSynonym("mã", "Code"); AddSynonym("ma", "Code"); AddSynonym("id", "Id");
        AddSynonym("tên", "Name"); AddSynonym("ten", "Name"); AddSynonym("hoten", "FullName");
        AddSynonym("họ tên", "FullName"); AddSynonym("ho va ten", "FullName"); AddSynonym("họ và tên", "FullName");
        AddSynonym("hovaten", "FullName"); AddSynonym("fullname", "FullName"); AddSynonym("full name", "FullName");
        AddSynonym("sdt", "Phone"); AddSynonym("số điện thoại", "Phone"); AddSynonym("so dien thoai", "Phone");
        AddSynonym("dienthoai", "Phone"); AddSynonym("điện thoại", "Phone"); AddSynonym("mobile", "Phone");
        AddSynonym("email", "Email"); AddSynonym("e-mail", "Email"); AddSynonym("thư điện tử", "Email");
        AddSynonym("phòng ban", "Department"); AddSynonym("phong ban", "Department"); AddSynonym("department", "Department");
        AddSynonym("dept", "Department"); AddSynonym("phòng", "Department"); AddSynonym("phong", "Department");
        AddSynonym("bộ phận", "Department"); AddSynonym("bo phan", "Department");
        AddSynonym("chức vụ", "Position"); AddSynonym("chuc vu", "Position"); AddSynonym("position", "Position");
        AddSynonym("vị trí", "Position"); AddSynonym("vi tri", "Position");

        // Department names
        AddSynonym("p. kỹ thuật", "Phòng Kỹ thuật"); AddSynonym("phòng kỹ thuật", "Phòng Kỹ thuật");
        AddSynonym("p kỹ thuật", "Phòng Kỹ thuật"); AddSynonym("p ky thuat", "Phòng Kỹ thuật");
        AddSynonym("kỹ thuật", "Phòng Kỹ thuật"); AddSynonym("ky thuat", "Phòng Kỹ thuật");
        AddSynonym("IT", "Phòng Kỹ thuật"); AddSynonym("it", "Phòng Kỹ thuật");
        AddSynonym("p. nhân sự", "Phòng Nhân sự"); AddSynonym("phòng nhân sự", "Phòng Nhân sự");
        AddSynonym("p nhân sự", "Phòng Nhân sự"); AddSynonym("p nhan su", "Phòng Nhân sự");
        AddSynonym("nhân sự", "Phòng Nhân sự"); AddSynonym("nhan su", "Phòng Nhân sự");
        AddSynonym("HR", "Phòng Nhân sự"); AddSynonym("hr", "Phòng Nhân sự");
        AddSynonym("p. bảo vệ", "Phòng Bảo vệ"); AddSynonym("phòng bảo vệ", "Phòng Bảo vệ");
        AddSynonym("p bảo vệ", "Phòng Bảo vệ"); AddSynonym("p bao ve", "Phòng Bảo vệ");
        AddSynonym("bảo vệ", "Phòng Bảo vệ"); AddSynonym("bao ve", "Phòng Bảo vệ");

        // Position names
        AddSynonym("nhân viên", "Nhân viên"); AddSynonym("nhan vien", "Nhân viên");
        AddSynonym("nv", "Nhân viên"); AddSynonym("staff", "Nhân viên");
        AddSynonym("trưởng nhóm", "Trưởng nhóm"); AddSynonym("truong nhom", "Trưởng nhóm");
        AddSynonym("truongnhom", "Trưởng nhóm"); AddSynonym("team lead", "Trưởng nhóm");
        AddSynonym("leader", "Trưởng nhóm"); AddSynonym("trưởng phòng", "Trưởng nhóm");
        AddSynonym("bảo vệ", "Bảo vệ"); AddSynonym("bao ve", "Bảo vệ");
        AddSynonym("security guard", "Bảo vệ"); AddSynonym("guard", "Bảo vệ");

        // License plate abbreviations
        AddSynonym("biển số", "LicensePlate"); AddSynonym("bienso", "LicensePlate");
        AddSynonym("bien so xe", "LicensePlate"); AddSynonym("plate", "LicensePlate");
        AddSynonym("loại xe", "VehicleType"); AddSynonym("loai xe", "VehicleType");
    }

    public Dictionary<string, List<string>> ExportRegistry()
    {
        return _reverseMap.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
