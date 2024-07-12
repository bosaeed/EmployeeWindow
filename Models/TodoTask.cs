namespace EmployeeWindow.Models
{
    public class TodoTask
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string AssignedToId { get; set; }
        public User AssignedTo { get; set; }
        public string AssignedById { get; set; }
        public User AssignedBy { get; set; }
        public bool IsCompleted { get; set; }
    }
}
