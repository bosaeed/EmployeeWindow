using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace EmployeeWindow.Services
{
    public class HistoryService
    {
        private ConcurrentDictionary<string, ChatHistory> _history = new ConcurrentDictionary<string, ChatHistory>();

        public void Add(string conID, ChatMessageContent message)
        {
            _history[conID].Add(message);
            
        }
        public void AddChat(string conID , string lang)
        {
            _history.TryAdd(conID, new ChatHistory($"You are a helpful assistant that manages a project todo tasks list. Use the provided tools to add tasks. answer user with {(lang == "EN" ? "English" : "Arabic")} language"));
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
