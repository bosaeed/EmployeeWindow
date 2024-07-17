using EmployeeWindow.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EmployeeWindow.Plugins
{
    public class TaskAdmin
    {


        private readonly ILogger<ChatService> _logger;

        public TaskAdmin(ILogger<ChatService> logger)
        {

            _logger = logger;
        }

        [KernelFunction("add_task")]
        //[Description("add task")]
        //[return: Description("An array of lights")]
        public async Task<TaskArgs> AddTask(
            [Required]string description,
            string user_name ="")
        {

            _logger.LogInformation("******************Add Task Function Called");
            _logger.LogInformation($"Desc {description}");
            _logger.LogInformation($"Name {user_name}");

            //return $"add_task:{description}";
            return new TaskArgs
            {
                type = TaskType.add,
                Description = description,
                Name = user_name
            };
        }
    }
}
