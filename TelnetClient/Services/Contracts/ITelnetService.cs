using System.Threading.Tasks;
using TelnetClient.Models;

namespace TelnetClient.Services.Contracts
{
    public interface ITelnetService
    {
        Task ConnectAsync(string ipAddress, string username, string password, string loginPrompt, string passwordPrompt); 
        Task DisconnectAsync(); 
        bool IsConnected { get; } 
        Task<SubscriberInfo> GetSubscriberInfoAsync(string login);
    }
}