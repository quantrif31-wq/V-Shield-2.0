using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class EmployeeFaceModel
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string ModelFileName { get; set; } = string.Empty;

        public string ModelPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Employee Employee { get; set; } = null!;
    }
}
