using EmployeeWindow.Data;
using EmployeeWindow.Models;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace EmployeeWindow.Services
{
    public class ChatService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly MyDbContext _context;

        public ChatService(OpenAIClient openAIClient, MyDbContext context)
        {
            _openAIClient = openAIClient;
            _context = context;
        }

        public async Task<string> ProcessMessageAsync(string message, bool isAdmin, string currentUserId)
        {
            var chatClient = _openAIClient.GetChatClient("gpt-3.5-turbo");

            var addTaskTool = ChatTool.CreateFunctionTool(
                "AddTask",
                "Add a new task to the todo list",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""description"": {
                            ""type"": ""string"",
                            ""description"": ""The description of the task""
                        },
                        ""assignedToId"": {
                            ""type"": ""string"",
                            ""description"": ""The ID of the user to assign the task to""
                        }
                    },
                    ""required"": [""description"", ""assignedToId""]
                }")
            );

            var completeTaskTool = ChatTool.CreateFunctionTool(
                "CompleteTask",
                "Mark a task as completed",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""description"": {
                            ""type"": ""string"",
                            ""description"": ""The description of the task to complete""
                        }
                    },
                    ""required"": [""description""]
                }")
            );

            var getAssignedTasksTool = ChatTool.CreateFunctionTool(
                "GetAssignedTasks",
                "Get tasks assigned to the current user",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {},
                    ""required"": []
                }")
            );

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful assistant that manages a todo list. Use the provided functions to add, complete, or retrieve tasks."),
                new UserChatMessage(message)
            };

            ChatCompletionOptions options = new()
            {
                Tools = { addTaskTool, completeTaskTool, getAssignedTasksTool },
            };

            ChatCompletion completion = chatClient.CompleteChat(messages, options);

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                foreach (var toolCall in completion.ToolCalls)
                {
                    if (toolCall.FunctionName == "AddTask" && isAdmin)
                    {
                        var args = JsonSerializer.Deserialize<AddTaskArgs>(toolCall.FunctionArguments);
                        await AddTaskAsync(args.Description, args.AssignedToId, currentUserId);
                        return $"Task added: {args.Description}, assigned to user with ID {args.AssignedToId}";
                    }
                    else if (toolCall.FunctionName == "CompleteTask")
                    {
                        var args = JsonSerializer.Deserialize<CompleteTaskArgs>(toolCall.FunctionArguments);
                        var result = await CompleteTaskAsync(args.Description, currentUserId);
                        return result;
                    }
                    else if (toolCall.FunctionName == "GetAssignedTasks")
                    {
                        var tasks = await GetAssignedTasksAsync(currentUserId);
                        return $"Your assigned tasks: {string.Join(", ", tasks)}";
                    }
                }
            }

            return completion.ToString();
        }

        private async Task AddTaskAsync(string description, string assignedToId, string assignedById)
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
        }

        private async Task<string> CompleteTaskAsync(string description, string userId)
        {
            var task = await _context.TodoTasks.FirstOrDefaultAsync(t => t.Description == description && t.AssignedToId == userId);
            if (task != null)
            {
                task.IsCompleted = true;
                await _context.SaveChangesAsync();
                return $"Task completed: {description}";
            }
            return "Task not found or not assigned to you.";
        }

        private async Task<List<string>> GetAssignedTasksAsync(string userId)
        {
            return await _context.TodoTasks
                .Where(t => t.AssignedToId == userId && !t.IsCompleted)
                .Select(t => t.Description)
                .ToListAsync();
        }
    }

    public class AddTaskArgs
    {
        public string Description { get; set; }
        public string AssignedToId { get; set; }
    }

    public class CompleteTaskArgs
    {
        public string Description { get; set; }
    }
}