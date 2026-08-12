using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    // Bảng phân quyền cho Nhân viên
    [Table("Employee_Access_Permissions")]
    public class EmployeeAccessPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int GateId { get; set; }

        public bool IsAllowed { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // Bảng phân quyền cho Khách (Liên kết Visitor_Details)
    [Table("Visitor_Access_Permissions")]
    public class VisitorAccessPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VisitorDetailId { get; set; }

        [Required]
        public int GateId { get; set; }

        public bool IsAllowed { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // Quyền khu vực mặc định theo Chức vụ (Position). Nhân viên gắn chức vụ
    // sẽ kế thừa ngay các quyền này (resolve khi đọc, không cần vật liệu hóa).
    [Table("Position_Access_Permissions")]
    public class PositionAccessPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PositionId { get; set; }

        [Required]
        public int GateId { get; set; }

        public bool IsAllowed { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}