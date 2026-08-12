using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

/// <summary>Quyền qua cổng (Gate) mặc định theo vai trò tài khoản</summary>
[Table("Role_Gate_Access_Permissions")]
public class RoleGateAccessPermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public int GateId { get; set; }

    public bool IsAllowed { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Quyền qua cổng (Gate) tùy chỉnh riêng theo tài khoản (ghi đè mặc định vai trò)</summary>
[Table("User_Gate_Access_Permissions")]
public class UserGateAccessPermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int GateId { get; set; }

    public bool IsAllowed { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
