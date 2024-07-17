using EmployeeWindow.Data;
using EmployeeWindow.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EmployeeWindow.Plugins
{
    public class TaskManagment
    {


        private readonly ILogger<ChatService> _logger;

        public TaskManagment( ILogger<ChatService> logger)
        {

            _logger = logger;
        }




        [KernelFunction("complete_task")]
        //[Description("Gets a list of lights and their current state")]
        //[return: Description("An array of lights")]
        public async Task<TaskArgs> CompleteTask(
            [Description("descrption of the task")]string description ="")
        {
            _logger.LogInformation($"******************Complete Task Function Called with description {description}");
            //return $"complete_task";
            return new TaskArgs
            {
               type = TaskType.complete,
               Description = description

            };
        }


        [KernelFunction("Retrieve_Tasks")]
        //[Description("Gets a list of lights and their current state")]
        //[return: Description("An array of lights")]
        public async Task<TaskArgs> RetrieveTasks()
        {
            _logger.LogInformation("******************Retrieve Task Function Called");
            //return "retrieve_tasks";
            return new TaskArgs
            {
                type = TaskType.retrive
            };
        }

    
    }
}
