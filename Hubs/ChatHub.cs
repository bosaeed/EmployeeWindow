using Microsoft.AspNetCore.SignalR;
using EmployeeWindow.Services;
using EmployeeWindow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;



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
            _history.AddChat(Context.ConnectionId, user);
            await Clients.Caller.ReceiveInfo(user);
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
            _logger.LogInformation($"****userId: {userId}");
            bool isAdmin = Context.User.IsInRole("Admin"); // Adjust based on your authentication setup
            _logger.LogInformation($"****isAdmin: {isAdmin}");
            TaskArgs response = await _chatService.ProcessMessageAsync(message, isAdmin , Context.ConnectionId);

            if (response.type == TaskType.add)
            {
                string taskDescription = response.Description;
                var users = await _userManager.Users.Select(u => new { u.Id, u.FullName }).ToListAsync();

                if (!string.IsNullOrEmpty(response.Name))
                {

                    _logger.LogInformation("*********************COHERE******************************");
                    string[] fullNamesArray = users.Select(u => u.FullName).ToArray();

                    if (fullNamesArray.Length > 1)
                    {

                        RerankResponse result = await _cohereService.RerankAsync(response.Name, fullNamesArray);


                        var rerankedNamesMap = result.Results.ToDictionary(r => r.Document.Text, r => r.Relevance_score);

                        // Sort users based on reranked relevance scores
                        var sortedUsers = users
                            .OrderByDescending(u => rerankedNamesMap.ContainsKey(u.FullName) ? rerankedNamesMap[u.FullName] : 0)
                            .Select(u => new { u.Id, u.FullName });

                        users = sortedUsers.ToList();

                    }
                    else
                    {
                        _logger.LogInformation("only one result or less no need rerank.");

                    }

                }


                await Clients.Caller.ReceivedAddTask(taskDescription, users);
            }
            else if (response.type == TaskType.retrive)
            {
                var tasks = await _chatService.GetUserTasksAsync(userId ,response.Description);
                await Clients.Caller.ReceivedRetrieveTasks(tasks);
            }
            else if (response.type == TaskType.complete)
            {
                var tasks = await _chatService.GetUserTasksAsync(userId , response.Description);
                await Clients.Caller.ReceivedCompleteTask(tasks);
            }
            else
            {
                await Clients.Caller.ReceiveMessage(response.Description);
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

        Task ReceiveInfo(User user);

    }


}