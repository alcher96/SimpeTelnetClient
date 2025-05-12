using System.Threading.Tasks;

namespace TelnetClient.Services.Contracts
{
    public interface IMacVendorService
    {
        Task<string> GetVendorAsync(string macAddress);
    }
}