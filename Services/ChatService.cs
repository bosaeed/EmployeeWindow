using EmployeeWindow.Data;
using EmployeeWindow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// ChatService.cs
namespace EmployeeWindow.Services
{
    public class ChatService
    {
        private const float temprature = (float)0.3;
        private readonly MyDbContext _context;
        private readonly Kernel _kernel;
        private readonly ILogger<ChatService> _logger;
        private readonly HistoryService _history;

        public ChatService(Kernel kernel, MyDbContext context , ILogger<ChatService> logger, HistoryService history)
        {
            _kernel = kernel;
            _context = context;
            _logger = logger;
            _history = history;
        }

        public async Task<string> ProcessMessageAsync(string message, bool isAdmin, User currentUser , string conID)
        {
            //_history.AddChat(conID, currentUser.PreferredLanguage);
            _logger.LogInformation("***********************************HISTORY********************");
            _history.AddUserMessage(conID, message);
            var hi = _history.GetChatHistory(conID);
            foreach (var item in hi)
            {
                _logger.LogInformation($"{item.Role}: {item.Content}");
            }

            List<KernelFunction> functions = new List<KernelFunction>()
            {
                 _kernel.CreateFunctionFromMethod(AddTaskFunction, "AddTask", "Add task todo." ,new List<KernelParameterMetadata>
                 {
                     new KernelParameterMetadata("description")
                     {
                         Description = "task description",
                         IsRequired = true,
                         
                     }
                 }),

                _kernel.CreateFunctionFromMethod(RetrieveTaskFunction, "RetrieveTask", "Retrieve user's tasks.", new List<KernelParameterMetadata>()),

                _kernel.CreateFunctionFromMethod(CompleteTaskFunction, "CompleteTask", "Complete a task.", new List<KernelParameterMetadata>
                {
                  
                }),
            };

            _kernel.ImportPluginFromFunctions("HelperFunctions", functions);

            //_kernel.ImportPluginFromType

            _history.AddUserMessage(conID, message);

            IChatCompletionService chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
            };

            ChatMessageContent response = await chatCompletion.GetChatMessageContentAsync(
                _history.GetChatHistory(conID),
                executionSettings: openAIPromptExecutionSettings,
                kernel: _kernel);



            // Get function calls from the chat message content and quit the chat loop if no function calls are found.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            IEnumerable<FunctionCallContent> functionCalls = FunctionCallContent.GetFunctionCalls(response);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (!functionCalls.Any())
            {
                _history.AddAssistantMessage(conID, response.Content);
                return response.Content;
            }

            foreach (var functionCall in functionCalls)
            {
                try
                {
                    // Invoking the function
       
                    var resultContent = await functionCall.InvokeAsync(_kernel);
                    var functionCallItems = new ChatMessageContentItemCollection()
                    {
                        functionCall,
                        
                    };
                    _history.Add(conID, new ChatMessageContent()
                    {
                        Role = AuthorRole.Assistant,
                        Items = functionCallItems
                    });
                    var functionResultItems = new ChatMessageContentItemCollection()
                    {
                        
                        resultContent
                    };
                    _history.Add(conID, new ChatMessageContent()
                    {
                        Role = AuthorRole.Tool,
                        Items = functionResultItems
                    });

                    _logger.LogInformation($"function result {resultContent.Result}");



                    return $"{resultContent.Result}";
                }
                catch (Exception ex)
                {

                    return "function error";
                }
            }

            return "no response";
        }

        public async Task<string> AddTaskFunction(string description)
        {

            _logger.LogInformation("******************Add Task Function Called");

            return $"add_task:{description}";
        }


        public async Task<string> AddTaskAsync(string description, string assignedToId, string assignedById)
        {
            var newTask = new TodoTask
            {
                Description = description,
                IsCompleted = false,
                AssignedToId = assignedToId,
                AssignedById = assignedById
            };
            _context.TodoTasks.Add(newTask);
            await _context.SaveChangesAsync();
            return $"Task added: {description}, assigned to user with ID {assignedToId}";
        }


        public async Task<string> RetrieveTaskFunction()
        {
            _logger.LogInformation("******************Retrieve Task Function Called");
            return "retrieve_tasks";
        }

        public async Task<List<TodoTask>> GetUserTasksAsync(string userId)
        {
            return await _context.TodoTasks
                .Where(t => t.AssignedToId == userId)
                .OrderBy(t => t.IsCompleted)
                .ToListAsync();
        }



        public async Task<string> CompleteTaskFunction()
        {
            _logger.LogInformation($"******************Complete Task Function Called ");
            return $"complete_task";
        }

   

        public async Task<string> CompleteTaskAsync(int taskId, string userId)
        {
            var task = await _context.TodoTasks.FindAsync(taskId);
            if (task == null)
            {
                return "Task not found.";
            }
            if (task.AssignedToId != userId)
            {
                return "You are not authorized to complete this task.";
            }
            task.IsCompleted = true;
            await _context.SaveChangesAsync();
            return $"Task '{task.Description}' has been marked as completed.";
        }
    }

    public class AddTaskArgs
    {
        public string Description { get; set; }
    }



}