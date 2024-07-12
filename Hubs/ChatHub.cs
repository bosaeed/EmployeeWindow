using Microsoft.AspNetCore.SignalR;
using EmployeeWindow.Services;
using EmployeeWindow.Models;
using Microsoft.AspNetCore.Identity;

namespace EmployeeWindow.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(string message)
        {
            bool isAdmin = Context.User.IsInRole("Admin"); // Adjust based on your authentication setup
            var response = await _chatService.ProcessMessageAsync(message, isAdmin , Context.UserIdentifier);
            await Clients.Caller.SendAsync("ReceiveMessage", response);
        }
    }
}