using Azure;
using EmployeeWindow.Data;
using EmployeeWindow.Models;
using EmployeeWindow.Plugins;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Threading.Tasks;

// ChatService.cs
namespace EmployeeWindow.Services
{
    public class ChatService
    {
        private readonly MyDbContext _context;
        private readonly Kernel _kernel;
        private readonly ILogger<ChatService> _logger;
        private readonly HistoryService _history;
        private readonly UserManager<User> _userManager;
        private readonly CohereService _cohereService;


        public ChatService(Kernel kernel, MyDbContext context , 
            ILogger<ChatService> logger, HistoryService history , 
            UserManager<User> userManager,CohereService cohereService)
        {
            _kernel = kernel;
            _context = context;
            _logger = logger;
            _history = history;
            _userManager = userManager;
            _cohereService = cohereService;
        }

        public async Task<TaskArgs> ProcessMessageAsync(string message, bool isAdmin , string conID)
        {
            //_history.AddChat(conID, currentUser.PreferredLanguage);
            _logger.LogInformation("*********************************HISTORY********************");
            _history.AddUserMessage(conID, message);
            var hi = _history.GetChatHistory(conID);
            foreach (var item in hi)
            {
                _logger.LogInformation($"{item.Role}: {item.Content}");
            }


            _kernel.ImportPluginFromType<TaskManagment>();

            if (isAdmin)
            {
                _kernel.ImportPluginFromType<TaskAdmin>();
            }


            _history.AddUserMessage(conID, message);

            IChatCompletionService chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
                Temperature = .5,

            };

            /*#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        GeminiPromptExecutionSettings openAIPromptExecutionSettings = new()
                        {
                            ToolCallBehavior = GeminiToolCallBehavior.EnableKernelFunctions,
                            Temperature = .5,

                        };
            #pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            */

            ChatMessageContent response = new ChatMessageContent();
            bool retry = true;
            int retryN = 0;

            while (retry)
            {
                retryN++;
                try
                {

                    response = await chatCompletion.GetChatMessageContentAsync(
                        _history.GetChatHistory(conID),
                        executionSettings: openAIPromptExecutionSettings,
                        kernel: _kernel);

                    retry=false;
                }
                catch (Exception ex)
                {
                    
                    if(retryN > 2)
                    {
                        return new TaskArgs
                        {
                            type = TaskType.no,
                            Description = "Error is happend, please try again"
                        };
                    }
                }
            }


            // Get function calls from the chat message content and quit the chat loop if no function calls are found.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            IEnumerable<FunctionCallContent> functionCalls = FunctionCallContent.GetFunctionCalls(response);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (!functionCalls.Any())
            {
                _history.AddAssistantMessage(conID, response.Content);
                return new TaskArgs
                {
                    type = TaskType.no,
                    Description = response.Content
                };
                //return response.Content;
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

                    var taskarg = (TaskArgs)resultContent.Result;

                    return taskarg;
                }
                catch (Exception ex)
                {

                    return null;
                }
            }

            return null;
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

            var user = await _userManager.Users.Select(u => u ).Where(u => u.Id == assignedToId).FirstOrDefaultAsync();
            return $"Task: {description}, assigned to {user.FullName}";
        }




        public async Task<List<TodoTask>> GetUserTasksAsync(string userId, string description = "")
        {
            try
            {
                _logger.LogInformation($"Getting tasks for user {userId} with description: {description}");

                var tasks = await _context.TodoTasks
                    .Where(t => t.AssignedToId == userId)
                    .OrderBy(t => t.IsCompleted)
                    .ToListAsync();

                _logger.LogInformation($"Found {tasks.Count} tasks for user {userId}");

                if (!string.IsNullOrEmpty(description) && tasks.Any())
                {
                    _logger.LogInformation("Starting Cohere reranking process");

                    string[] tasksArray = tasks.Select(t => t.Description).ToArray();

                    if (_cohereService == null)
                    {
                        _logger.LogError("CohereService is null. Check DI configuration.");
                        return tasks;
                    }

                    if(tasks.Count < 2)
                    {
                        _logger.LogInformation("only one result or less no need rerank.");
                        return tasks;
                    }
                    try
                    {
                        RerankResponse result = await _cohereService.RerankAsync(description, tasksArray);

                        if (result == null)
                        {
                            _logger.LogWarning("Rerank response is null");
                            return tasks;
                        }

                        if (result.Results == null)
                        {
                            _logger.LogWarning("Rerank results are null");
                            return tasks;
                        }

                        var rerankedTaskssMap = result.Results
                            .Where(r => r.Document != null && !string.IsNullOrEmpty(r.Document.Text))
                            .GroupBy(r => r.Document.Text)
                            .ToDictionary(
                                g => g.Key,
                                g => g.First().Relevance_score
                            );

                        tasks = tasks
                            .OrderByDescending(u => rerankedTaskssMap.ContainsKey(u.Description) ? rerankedTaskssMap[u.Description] : 0)
                            .ToList();

                        _logger.LogInformation("Cohere reranking process completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred during Cohere reranking process");
                    }
                }

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception in GetUserTasksAsync for user {userId}");
                throw; // Rethrow the exception after logging
            }
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

    public class TaskArgs { 
        
        public TaskType type { get; set; }
        public string Description { get; set; } = "";
        public string Name { get; set; } = "";

    }

    public enum TaskType
    {
        add,
        retrive,
        complete,
        no
    }

}