using System;
using System.IO;
using System.Text.Json;
using TelnetClient.Services.Contracts;

namespace TelnetClient.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath = "settings.json";
        private readonly ILoggingService _loggingService;

        public SettingsService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public Settings LoadSettings()
        {
            try
            {
                _loggingService.Log("Attempting to read settings.json");
                if (!File.Exists(_settingsFilePath))
                {
                    _loggingService.Log("settings.json not found");
                    throw new FileNotFoundException("settings.json not found");
                }

                string json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<Settings>(json) ?? throw new Exception("Failed to deserialize settings.json");
                _loggingService.Log("Loaded settings from settings.json");

                if (string.IsNullOrEmpty(settings.SwitchIp) || string.IsNullOrEmpty(settings.Username) ||
                    string.IsNullOrEmpty(settings.Password) || string.IsNullOrEmpty(settings.LoginPrompt) ||
                    string.IsNullOrEmpty(settings.PasswordPrompt))
                {
                    throw new Exception("Invalid configuration: one or more fields are empty.");
                }

                return settings;
            }
            catch (Exception ex)
            {
                _loggingService.Log($"Error loading settings: {ex.Message}");
                throw;
            }
        }

        public void SaveSettings(Settings settings)
        {
            try
            {
                _loggingService.Log("Saving settings to settings.json");
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
                _loggingService.Log("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _loggingService.Log($"Error saving settings: {ex.Message}");
                throw;
            }
        }
    }
}