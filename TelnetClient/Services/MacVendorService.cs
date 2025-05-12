using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TelnetClient.Services.Contracts;

namespace TelnetClient.Services
{
    public class MacVendorService : IMacVendorService
    {
        private readonly ILoggingService _loggingService;

        public MacVendorService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public async Task<string> GetVendorAsync(string macAddress)
        {
            string formattedMac = macAddress.ToUpper().Replace(":", "-");
            string apiUrl = $"https://api.macvendors.com/{formattedMac}";
            _loggingService.Log($"Querying MacVendors API for MAC: {formattedMac}");

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                using (var web = new WebClient())
                {
                    web.Proxy = null;
                    string vendor = await web.DownloadStringTaskAsync(apiUrl);
                    _loggingService.Log($"MacVendor response: {vendor}");
                    return vendor;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Log($"MacVendor error: {ex.Message}");
                return "Not found";
            }
        }
    }
}