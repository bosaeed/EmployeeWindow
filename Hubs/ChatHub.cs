using Microsoft.AspNetCore.SignalR;
using EmployeeWindow.Services;
using EmployeeWindow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;


// ChatHub.cs
namespace EmployeeWindow.Hubs
{
    public class ChatHub : Hub<IChatClient>
    {
        private readonly ChatService _chatService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ChatHub> _logger;
        private readonly HistoryService _history;
        private readonly CohereService _cohereService;

        public ChatHub(ChatService chatService, UserManager<User> userManager, ILogger<ChatHub> logger, HistoryService history, CohereService cohereService)
        {
            _chatService = chatService;
            _userManager = userManager;
            _logger = logger;
            _history = history;
            _cohereService = cohereService;
        }


        public override async Task<Task> OnConnectedAsync()
        {
            _logger.LogInformation($"Connected hub for user: {Context.User.Identity.Name}");
            var user = await _userManager.GetUserAsync(Context.User);
            _history.AddChat(Context.ConnectionId, user.PreferredLanguage);
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"disConnected hub for user: {Context.User.Identity.Name}");
            _history.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string message)
        {
            _logger.LogInformation($"ConnextioID: {Context.ConnectionId}");
            _logger.LogInformation($"UserIdentifier: {Context.UserIdentifier}");
            _logger.LogInformation($"Message: {message}");
            var user = await _userManager.GetUserAsync(Context.User);
            var userId = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation($"userId: {userId}");
            bool isAdmin = Context.User.IsInRole("Admin"); // Adjust based on your authentication setup
            var response = await _chatService.ProcessMessageAsync(message, isAdmin, user , Context.ConnectionId);

            if (response.StartsWith("add_task:"))
            {
                string taskDescription = response.Substring(9);
                var users = await _userManager.Users.Select(u => new { u.Id, u.FullName }).ToListAsync();


                _logger.LogInformation("*********************COHERE******************************");
                string[] fullNamesArray = users.Select(u => u.FullName).ToArray();
                _logger.LogInformation(fullNamesArray.ToString());
                var result = await _cohereService.RerankAsync("Mohmmed", fullNamesArray);
                string json = JsonConvert.SerializeObject(result);

                // Now 'json' contains the serialized RerankResponse
                _logger.LogInformation(json);

                await Clients.Caller.ReceivedAddTask(taskDescription, users);
            }
            else if (response == "retrieve_tasks")
            {
                var tasks = await _chatService.GetUserTasksAsync(userId);
                await Clients.Caller.ReceivedRetrieveTasks(tasks);
            }
            else if (response == "complete_task")
            {
                var tasks = await _chatService.GetUserTasksAsync(userId);
                await Clients.Caller.ReceivedCompleteTask(tasks);
            }
            else
            {
                await Clients.Caller.ReceiveMessage(response);
            }

        }



        public async Task SendAddTask(string description, string assignedToId)
        {
            _logger.LogInformation($"ConnextioID: {Context.ConnectionId}");
            _logger.LogInformation($"UserIdentifier: {Context.UserIdentifier}");
            var user = await _userManager.GetUserAsync(Context.User);
            var userId = await _userManager.GetUserIdAsync(user);
            var result = await _chatService.AddTaskAsync(description, assignedToId, userId);
            await Clients.Caller.ReceiveMessage(result);
        }


        public async Task SendCompleteTask(int taskId)
        {
            _logger.LogInformation($"ConnextioID: {Context.ConnectionId}");
            _logger.LogInformation($"UserIdentifier: {Context.UserIdentifier}");
            var user = await _userManager.GetUserAsync(Context.User);
            var userId = await _userManager.GetUserIdAsync(user);
            var result = await _chatService.CompleteTaskAsync(taskId, userId);
            await Clients.Caller.ReceiveMessage(result);
        }

    }

    public interface IChatClient
    {
        Task ReceiveMessage(string message);
        Task ReceivedAddTask(string taskDescription, IEnumerable<dynamic> users);
        Task ReceivedRetrieveTasks(List<TodoTask> tasks);

        Task ReceivedCompleteTask(List<TodoTask> tasks);

    }


}