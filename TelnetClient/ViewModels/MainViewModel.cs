using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using TelnetClient.Models;
using TelnetClient.Services;
using TelnetClient.Services.Contracts;

namespace TelnetClient.ViewModels
{
    public class MainViewModel
    {
        private readonly ITelnetService _telnetService;
        private readonly IMacVendorService _macVendorService;
        private readonly ISettingsService _settingsService;
        private readonly ILoggingService _loggingService;

        public MainViewModel(ITelnetService telnetService, IMacVendorService macVendorService, ISettingsService settingsService, ILoggingService loggingService)
        {
            _telnetService = telnetService;
            _macVendorService = macVendorService;
            _settingsService = settingsService;
            _loggingService = loggingService;
        }

        public Settings Settings => _settingsService.LoadSettings();

        public async Task ConnectAsync(string ipAddress)
        {
            var settings = _settingsService.LoadSettings();
            string decryptedPassword = CryptoHelper.Decrypt(settings.Password);
            await _telnetService.ConnectAsync(ipAddress, settings.Username, decryptedPassword, settings.LoginPrompt, settings.PasswordPrompt);
        }

        public async Task DisconnectAsync()
        {
            await _telnetService.DisconnectAsync();
        }

        public async Task<SubscriberInfo> CheckAuthorizationAsync(string login)
        {
            if (!_telnetService.IsConnected)
            {
                _loggingService.Log("Error: Not connected to the switch");
                throw new InvalidOperationException("Not connected");
            }

            if (string.IsNullOrEmpty(login))
            {
                _loggingService.Log("Error: Login field is empty");
                throw new ArgumentException("Login field is empty");
            }

            var info = await _telnetService.GetSubscriberInfoAsync(login);
            if (info != null && !string.IsNullOrEmpty(info.MacAddress))
            {
                info.Vendor = await _macVendorService.GetVendorAsync(info.MacAddress);
            }

            return info;
        }

        public void ShowOptionsForm(Action<Settings> onSaveSettings)
        {
            using (var optionsForm = new OptionsForm(Settings, onSaveSettings))
            {
                if (optionsForm.ShowDialog() == DialogResult.OK)
                {
                    _loggingService.Log("Configuration updated from Options form");
                }
            }
        }

        public void SaveSettings(Settings settings)
        {
            _settingsService.SaveSettings(settings);
        }
    }
}