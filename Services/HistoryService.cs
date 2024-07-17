using EmployeeWindow.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace EmployeeWindow.Services
{
    public class HistoryService
    {
        private ConcurrentDictionary<string, ChatHistory> _history = new ConcurrentDictionary<string, ChatHistory>();

        public void AddChat(string conID , User user)
        {
            if(!_history.ContainsKey(conID))
            {

                _history.TryAdd(conID, new ChatHistory($"You are a helpful assistant that manages a project tasks. Use the provided tools manage tasks. answer user with {(user.PreferredLanguage == "EN" ? "English" : "Arabic")} language, say i do not know if not know the answer or do not have required tool"));
                AddAssistantMessage(conID, $@"User Information:
user name: {user.FullName}");
            }
        }

        public void Add(string conID, ChatMessageContent message)
        {
            _history[conID].Add(message);
            
        }
        public void AddUserMessage(string conID , string message)
        {
            _history[conID].AddUserMessage(message);
        }

        public void AddAssistantMessage(string conID , string message)
        {
            _history[conID].AddAssistantMessage(message);
        }

        public ChatHistory GetChatHistory(string conID)
        {
            return _history[conID];
        }

        public void Remove(string conID)
        {
            _history.TryRemove(conID, out _);
        }
    }
}
