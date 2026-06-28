using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CreateUserRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [MaxLength(100)]
    public string? FullName { get; set; }

    /// <summary>Admin | Staff | BaoVe | QuanLy | LeTan</summary>
    [Required]
    [RegularExpression("^(Admin|Staff|BaoVe|QuanLy|LeTan)$", ErrorMessage = "Role phai la Admin, Staff, BaoVe, QuanLy hoac LeTan")]
    public string Role { get; set; } = "Staff";

    public int? EmployeeId { get; set; }
}
