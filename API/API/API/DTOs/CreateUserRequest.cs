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

    /// <summary>Admin | QuanLy | BaoVe | LeTan</summary>
    [Required]
    [RegularExpression("^(Admin|BaoVe|QuanLy|LeTan)$", ErrorMessage = "Role phai la Admin, BaoVe, QuanLy hoac LeTan")]
    public string Role { get; set; } = "LeTan";

    public int? EmployeeId { get; set; }
}
