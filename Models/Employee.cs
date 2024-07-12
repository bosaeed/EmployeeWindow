using System.ComponentModel.DataAnnotations;

namespace EmployeeWindow.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }


        public string? Description { get; set; }
        public int EmployeeId { get; set; }
    }
}
