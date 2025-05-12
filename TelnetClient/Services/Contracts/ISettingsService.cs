namespace TelnetClient.Services.Contracts
{
    public interface ISettingsService
    {
        Settings LoadSettings(); 
        void SaveSettings(Settings settings);
    }
}