namespace TelnetClient.Services.Contracts
{
    public interface ILoggingService
    {
        void Log(string message); 
        void LogRawData(string data);
    }
}