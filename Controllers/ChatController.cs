using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using EmployeeWindow.Models;
using EmployeeWindow.Services;
using Microsoft.AspNetCore.SignalR;
using EmployeeWindow.Hubs;

namespace EmployeeWindow.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(UserManager<User> userManager, ChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _userManager = userManager;
            _chatService = chatService;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessMessage(string message)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = await _userManager.GetUserIdAsync(user);
            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var response = await _chatService.ProcessMessageAsync(message, isAdmin , userId);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", user.UserName, message);
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "System", response);

            return Json(new { success = true });
        }
    }
}