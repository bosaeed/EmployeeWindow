namespace EmployeeWindow.Models
{
    public class TodoTask
    {

        public int Id { get; set; }
        public string Description { get; set; }
        public string AssignedToId { get; set; } // Foreign key for AssignedTo user
        public string AssignedById { get; set; } // Foreign key for AssignedBy user
        public bool IsCompleted { get; set; }


    }
}
