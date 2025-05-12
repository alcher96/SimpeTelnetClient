using PrimS.Telnet;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TelnetClient.Models;
using TelnetClient.Services.Contracts;

namespace TelnetClient.Services
{
    public class TelnetService : ITelnetService, IDisposable
    {
        private Client _telnet;
        private readonly ILoggingService _loggingService;
        private bool _isConnected;

        public TelnetService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public bool IsConnected => _isConnected;

        public async Task ConnectAsync(string ipAddress, string username, string password, string loginPrompt, string passwordPrompt)
        {
            _loggingService.Log($"Attempting to connect to {ipAddress}:23");
            _telnet = new Client(ipAddress, 23, new CancellationToken());
            if (!_telnet.IsConnected)
            {
                _loggingService.Log("Connection failed");
                throw new Exception("Connection failed");
            }

            _isConnected = true;
            _loggingService.Log("Connected successfully");

            // Ожидание Login:
            string response = await _telnet.ReadAsync(TimeSpan.FromSeconds(10));
            _loggingService.Log($"Received: {response.Trim()}");
            _loggingService.LogRawData(response);
            if (string.IsNullOrEmpty(response) || !response.Contains(loginPrompt, StringComparison.OrdinalIgnoreCase))
            {
                _loggingService.Log($"Error: Login prompt not received, expected: {loginPrompt}");
                await DisconnectAsync();
                throw new Exception("Login prompt not received");
            }

            // Отправка логина
            await _telnet.WriteLineAsync(username);
            _loggingService.Log($"Sent login: {username}");

            // Ожидание Password:
            response = await _telnet.ReadAsync(TimeSpan.FromSeconds(5));
            _loggingService.Log($"Received: {response.Trim()}");
            _loggingService.LogRawData(response);
            if (string.IsNullOrEmpty(response) || !response.Contains(passwordPrompt, StringComparison.OrdinalIgnoreCase))
            {
                _loggingService.Log($"Error: Password prompt not received, expected: {passwordPrompt}");
                await DisconnectAsync();
                throw new Exception("Password prompt not received");
            }

            // Отправка пароля
            await _telnet.WriteLineAsync(password);
            _loggingService.Log("Sent password: [hidden]");

            // Проверка успешности логина
            response = await _telnet.ReadAsync(TimeSpan.FromSeconds(5));
            _loggingService.Log($"Received: {response.Trim()}");
            _loggingService.LogRawData(response);
            if (!response.Contains("#") && !response.Contains(">"))
            {
                _loggingService.Log("Login failed");
                await DisconnectAsync();
                throw new Exception("Login failed");
            }

            _loggingService.Log("Login successful");
        }

        public async Task DisconnectAsync()
        {
            if (_telnet != null)
            {
                _telnet.Dispose();
                _telnet = null;
            }
            _isConnected = false;
            _loggingService.Log("Disconnected");
        }

        public async Task<SubscriberInfo> GetSubscriberInfoAsync(string login)
        {
            if (!_isConnected || _telnet == null)
            {
                _loggingService.Log("Error: Not connected to the switch");
                throw new InvalidOperationException("Not connected");
            }

            // Отправка команды show subscribers
            string command = $"show subscribers | match {login}";
            _loggingService.Log($"Sending command: {command}");
            await _telnet.WriteLineAsync(command);
            await _telnet.WriteLineAsync("");

            // Ожидание ответа
            string response = await ReadResponseAsync("admin@MPLS-CORE_2>");
            _loggingService.Log($"Full response: {response}");

            // Фильтрация ответа
            var lines = response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            StringBuilder filteredLines = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("pp0."))
                {
                    filteredLines.AppendLine(line.Trim());
                }
            }

            string filteredResponse = filteredLines.ToString().Trim();
            _loggingService.Log($"Filtered response: {filteredResponse}");

            if (string.IsNullOrEmpty(filteredResponse))
            {
                _loggingService.Log("No subscribers found or command returned no data");
                return null;
            }

            var info = new SubscriberInfo();

            // Извлечение интерфейса
            var interfaceMatch = Regex.Match(filteredResponse, @"pp0\.\d+");
            if (interfaceMatch.Success)
            {
                info.Interface = interfaceMatch.Value;
                _loggingService.Log($"Interface extracted: {info.Interface}");
            }
            else
            {
                _loggingService.Log("Error: Interface (pp0.<number>) not found in response");
                return null;
            }

            // Извлечение IP-адреса
            var ipMatch = Regex.Match(filteredResponse, @"\b(?:\d{1,3}\.){3}\d{1,3}\b");
            if (ipMatch.Success)
            {
                info.IpAddress = ipMatch.Value;
                _loggingService.Log($"IP extracted: {info.IpAddress}");
            }
            else
            {
                _loggingService.Log("Error: IP address not found in response");
            }

            // Отправка команды show pppoe interfaces
            await Task.Delay(500);
            string pppoeCommand = $"show pppoe interfaces {info.Interface}";
            _loggingService.Log($"Sending PPPoE command: {pppoeCommand}");
            await _telnet.WriteLineAsync(pppoeCommand);
            await _telnet.WriteLineAsync("");

            // Ожидание ответа
            string pppoeResponse = await ReadResponseAsync("admin@MPLS-CORE_2>");
            _loggingService.Log($"PPPoE Full response: {pppoeResponse}");

            // Фильтрация PPPoE ответа
            var pppoeLines = pppoeResponse.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            filteredLines.Clear();
            foreach (var line in pppoeLines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("pp0.") || trimmedLine.Contains("Remote MAC address") || trimmedLine.Contains("Session uptime"))
                {
                    filteredLines.AppendLine(trimmedLine);
                }
            }

            string pppoeFilteredResponse = filteredLines.ToString().Trim();
            _loggingService.Log($"PPPoE Filtered response: {pppoeFilteredResponse}");

            if (string.IsNullOrEmpty(pppoeFilteredResponse))
            {
                _loggingService.Log("No PPPoE data received");
                return info;
            }

            // Извлечение MAC-адреса
            var macMatch = Regex.Match(pppoeFilteredResponse, @"Remote MAC address: ([0-9A-F:]{17})");
            if (macMatch.Success)
            {
                info.MacAddress = macMatch.Groups[1].Value;
                _loggingService.Log($"MAC extracted: {info.MacAddress}");
            }
            else
            {
                _loggingService.Log("Error: MAC address not found in PPPoE response");
            }

            // Извлечение Uptime
            var uptimeMatch = Regex.Match(pppoeFilteredResponse, @"Session uptime: [^\r\n]+");
            if (uptimeMatch.Success)
            {
                info.Uptime = uptimeMatch.Value;
                _loggingService.Log($"Uptime extracted: {info.Uptime}");
            }
            else
            {
                _loggingService.Log("Error: Session uptime not found in PPPoE response");
            }

            return info;
        }

        private async Task<string> ReadResponseAsync(string endMarker)
        {
            StringBuilder response = new StringBuilder();
            bool responseReceived = false;
            for (int i = 0; i < 240; i++)
            {
                string chunk = await _telnet.ReadAsync(TimeSpan.FromSeconds(0.5));
                if (!string.IsNullOrEmpty(chunk))
                {
                    response.Append(chunk);
                    _loggingService.Log($"Received (partial): {chunk.Trim()}");
                    _loggingService.LogRawData(chunk);
                    responseReceived = true;
                }

                if (response.ToString().Contains(endMarker))
                {
                    for (int j = 0; j < 20; j++)
                    {
                        chunk = await _telnet.ReadAsync(TimeSpan.FromSeconds(0.5));
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            response.Append(chunk);
                            _loggingService.Log($"Received (additional): {chunk.Trim()}");
                            _loggingService.LogRawData(chunk);
                            responseReceived = true;
                        }
                    }
                    break;
                }
            }

            if (!responseReceived)
            {
                _loggingService.Log("Error: No response received");
                throw new Exception("No response received");
            }

            return response.ToString().Trim();
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}