using System;
using System.IO;
using System.Text;
using TelnetClient.Services.Contracts;

namespace TelnetClient.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly string _logFilePath = "telnet_log.txt";

        public void Log(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
            File.AppendAllText(_logFilePath, logMessage);
        }

        public void LogRawData(string data)
        {
            string hex = BitConverter.ToString(Encoding.UTF8.GetBytes(data)).Replace("-", " ");
            Log($"Raw data: {hex}");
        }
    }
}