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

    }
}