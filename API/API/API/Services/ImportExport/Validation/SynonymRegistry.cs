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
        if (string.IsNullOrWhiteSpace(value))
            return null;

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
            if (trimmed is "1")
                return "true";
            if (trimmed is "0")
                return "false";
        }

        return null;
    }

    public List<string> GetSynonyms(string standardValue) =>
        _reverseMap.TryGetValue(standardValue, out var synonyms) ? synonyms : [standardValue];

    public bool IsStandardValue(string value, string standard)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (string.Equals(value.Trim(), standard, StringComparison.OrdinalIgnoreCase))
            return true;

        var found = FindStandard(value);
        return found != null && string.Equals(found, standard, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForMatch(string s) =>
        s.Trim()
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("_", "")
            .Replace("-", "")
            .Replace("/", "");

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
        AddSynonym("true", "true"); AddSynonym("TRUE", "true"); AddSynonym("True", "true");
        AddSynonym("1", "true"); AddSynonym("yes", "true"); AddSynonym("YES", "true");
        AddSynonym("co", "true"); AddSynonym("x", "true"); AddSynonym("active", "true");
        AddSynonym("false", "false"); AddSynonym("FALSE", "false"); AddSynonym("False", "false");
        AddSynonym("0", "false"); AddSynonym("no", "false"); AddSynonym("NO", "false");
        AddSynonym("khong", "false"); AddSynonym("inactive", "false"); AddSynonym("", "false");

        AddSynonym("admin", "Admin"); AddSynonym("quan tri", "Admin"); AddSynonym("quan tri vien", "Admin");
        AddSynonym("staff", "Staff"); AddSynonym("nhan vien", "Staff"); AddSynonym("nv", "Staff"); AddSynonym("employee", "Staff");
        AddSynonym("quan ly", "QuanLy"); AddSynonym("manager", "QuanLy");
        AddSynonym("bao ve", "BaoVe"); AddSynonym("baove", "BaoVe"); AddSynonym("security", "BaoVe"); AddSynonym("guard", "BaoVe");
        AddSynonym("le tan", "LeTan"); AddSynonym("letan", "LeTan"); AddSynonym("reception", "LeTan"); AddSynonym("receptionist", "LeTan");

        AddSynonym("active", "Active"); AddSynonym("hoat dong", "Active"); AddSynonym("dang lam viec", "Active");
        AddSynonym("inactive", "Inactive"); AddSynonym("ngung hoat dong", "Inactive");
        AddSynonym("suspended", "Suspended"); AddSynonym("tam ngung", "Suspended");
        AddSynonym("terminated", "Terminated"); AddSynonym("nghi viec", "Terminated"); AddSynonym("resigned", "Terminated");

        AddSynonym("male", "Male"); AddSynonym("nam", "Male");
        AddSynonym("female", "Female"); AddSynonym("nu", "Female");

        AddSynonym("stt", "No"); AddSynonym("so thu tu", "No");
        AddSynonym("ma", "Code"); AddSynonym("id", "Id");
        AddSynonym("ten", "Name"); AddSynonym("hoten", "FullName"); AddSynonym("ho va ten", "FullName"); AddSynonym("fullname", "FullName");
        AddSynonym("sdt", "Phone"); AddSynonym("so dien thoai", "Phone"); AddSynonym("dienthoai", "Phone"); AddSynonym("mobile", "Phone");
        AddSynonym("email", "Email"); AddSynonym("e-mail", "Email");
        AddSynonym("phong ban", "Department"); AddSynonym("department", "Department"); AddSynonym("dept", "Department");
        AddSynonym("chuc vu", "Position"); AddSynonym("position", "Position"); AddSynonym("vi tri", "Position");

        AddSynonym("bien so", "LicensePlate"); AddSynonym("plate", "LicensePlate");
        AddSynonym("loai xe", "VehicleType");
    }

    public Dictionary<string, List<string>> ExportRegistry()
    {
        return _reverseMap.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
