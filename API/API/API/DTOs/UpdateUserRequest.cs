using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class UpdateUserRequest
{
    [MaxLength(100)]
    public string? FullName { get; set; }

    /// <summary>Admin | Staff | BaoVe | QuanLy | LeTan</summary>
    [RegularExpression("^(Admin|Staff|BaoVe|QuanLy|LeTan)$", ErrorMessage = "Role phai la Admin, Staff, BaoVe, QuanLy hoac LeTan")]
    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>De trong neu khong muon doi mat khau</summary>
    [MinLength(6)]
    public string? Password { get; set; }

    public int? EmployeeId { get; set; }
}
