using System;
using System.Windows.Forms;
using TelnetClient.Models;
using TelnetClient.Services;
using TelnetClient.Services.Contracts;
using TelnetClient.ViewModels;

namespace TelnetClient
{
    public partial class Form1 : Form
    {
        private readonly MainViewModel _viewModel;
        private TextBox ipTextBox;
        private Button connectButton;
        private Button disconnectButton;
        private Label statusLabel;
        private TextBox loginTextBox;
        private TextBox interfaceTextBox;
        private TextBox ipAddressTextBox;
        private TextBox macTextBox;
        private TextBox uptimeTextBox;
        private Label MacVendorLbl;
        private Button checkAuthButton;
        private MenuStrip menuStrip;

        public Form1()
        {
            // Инициализация сервисов и ViewModel
            ILoggingService loggingService = new LoggingService();
            ISettingsService settingsService = new SettingsService(loggingService);
            ITelnetService telnetService = new TelnetService(loggingService);
            IMacVendorService macVendorService = new MacVendorService(loggingService);
            _viewModel = new MainViewModel(telnetService, macVendorService, settingsService, loggingService);

            InitializeComponents();

            // Загрузка настроек
            try
            {
                var settings = _viewModel.Settings;
                UpdateIpTextBox(settings.SwitchIp);
            }
            catch (FileNotFoundException)
            {
                _viewModel.ShowOptionsForm(settings => _viewModel.SaveSettings(settings));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}\nApplication will close.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void InitializeComponents()
        {
            // Инициализация MenuStrip
            menuStrip = new MenuStrip();
            ToolStripMenuItem optionsMenu = new ToolStripMenuItem("Options");
            optionsMenu.Click += (s, e) => _viewModel.ShowOptionsForm(settings =>
            {
                _viewModel.SaveSettings(settings);
                UpdateIpTextBox(settings.SwitchIp);
                UpdateStatusLabel("Configuration updated");
            });
            menuStrip.Items.Add(optionsMenu);
            this.Controls.Add(menuStrip);

            // Инициализация UI элементов
            ipTextBox = new TextBox { Location = new System.Drawing.Point(10, 30), Width = 200, PlaceholderText = "IP Address" };
            connectButton = new Button { Text = "Connect", Location = new System.Drawing.Point(10, 60), Width = 100, Height = 30 };
            disconnectButton = new Button { Text = "Disconnect", Location = new System.Drawing.Point(120, 60), Width = 100, Height = 30 };
            statusLabel = new Label { Text = "Status: Not connected", Location = new System.Drawing.Point(10, 100), Width = 200 };
            loginTextBox = new TextBox { Location = new System.Drawing.Point(10, 130), Width = 200, PlaceholderText = "Login" };
            interfaceTextBox = new TextBox { Location = new System.Drawing.Point(10, 160), Width = 200, PlaceholderText = "Interface", ReadOnly = true };
            ipAddressTextBox = new TextBox { Location = new System.Drawing.Point(10, 190), Width = 200, PlaceholderText = "IP Address", ReadOnly = true };
            macTextBox = new TextBox { Location = new System.Drawing.Point(10, 220), Width = 200, PlaceholderText = "MAC Address", ReadOnly = true };
            uptimeTextBox = new TextBox { Location = new System.Drawing.Point(10, 250), Width = 200, PlaceholderText = "Uptime", ReadOnly = true };
            MacVendorLbl = new Label { Text = "Vendor: Unknown", Location = new System.Drawing.Point(10, 280), Width = 200 };
            checkAuthButton = new Button { Text = "Check Authorization", Location = new System.Drawing.Point(10, 310), Width = 200, Height = 30 };

            // Добавление обработчиков событий
            connectButton.Click += async (s, e) => await ConnectButton_ClickAsync();
            disconnectButton.Click += async (s, e) => await DisconnectButton_ClickAsync();
            checkAuthButton.Click += async (s, e) => await CheckAuthButton_ClickAsync();
            loginTextBox.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await CheckAuthButton_ClickAsync();
                }
            };
            loginTextBox.TextChanged += (s, e) =>
            {
                UpdateInterfaceTextBox("");
                UpdateIpAddressTextBox("");
                UpdateMacTextBox("");
                UpdateUptimeTextBox("");
                UpdateMacVendorLabel("Unknown");
            };

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[] { ipTextBox, connectButton, disconnectButton, statusLabel,
                loginTextBox, interfaceTextBox, ipAddressTextBox, macTextBox, uptimeTextBox, MacVendorLbl, checkAuthButton });

            // Настройки формы
            this.Text = "Telnet Client";
            this.Size = new System.Drawing.Size(250, 410);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private async Task ConnectButton_ClickAsync()
        {
            if (string.IsNullOrEmpty(ipTextBox.Text))
            {
                UpdateStatusLabel("IP address is empty");
                return;
            }

            try
            {
                UpdateStatusLabel("Connecting...");
                await _viewModel.ConnectAsync(ipTextBox.Text);
                UpdateStatusLabel("Connected successfully");
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Error - {ex.Message}");
                await _viewModel.DisconnectAsync();
            }
        }

        private async Task DisconnectButton_ClickAsync()
        {
            try
            {
                await _viewModel.DisconnectAsync();
                UpdateStatusLabel("Disconnected");
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Disconnect error - {ex.Message}");
            }
        }

        private async Task CheckAuthButton_ClickAsync()
        {
            try
            {
                UpdateStatusLabel("Sending subscribers command...");
                var info = await _viewModel.CheckAuthorizationAsync(loginTextBox.Text.Trim());
                if (info == null)
                {
                    UpdateStatusLabel("No subscribers found");
                    return;
                }

                UpdateInterfaceTextBox(info.Interface ?? "");
                UpdateIpAddressTextBox(info.IpAddress ?? "");
                UpdateMacTextBox(info.MacAddress ?? "");
                UpdateUptimeTextBox(info.Uptime ?? "");
                UpdateMacVendorLabel(info.Vendor ?? "Unknown");

                if (!string.IsNullOrEmpty(info.MacAddress) && !string.IsNullOrEmpty(info.Uptime))
                {
                    UpdateStatusLabel("MAC and uptime extracted");
                }
                else
                {
                    UpdateStatusLabel("PPPoE response received");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Check Authorization error - {ex.Message}");
                await _viewModel.DisconnectAsync();
            }
        }

        private void UpdateIpTextBox(string ipValue)
        {
            if (ipTextBox.InvokeRequired)
                ipTextBox.Invoke(new Action(() => ipTextBox.Text = ipValue));
            else
                ipTextBox.Text = ipValue;
        }

        private void UpdateIpAddressTextBox(string ipAddressValue)
        {
            if (ipAddressTextBox.InvokeRequired)
                ipAddressTextBox.Invoke(new Action(() => ipAddressTextBox.Text = ipAddressValue));
            else
                ipAddressTextBox.Text = ipAddressValue;
        }

        private void UpdateStatusLabel(string status)
        {
            if (statusLabel.InvokeRequired)
                statusLabel.Invoke(new Action(() => statusLabel.Text = $"Status: {status}"));
            else
                statusLabel.Text = $"Status: {status}";
        }

        private void UpdateInterfaceTextBox(string interfaceValue)
        {
            if (interfaceTextBox.InvokeRequired)
                interfaceTextBox.Invoke(new Action(() => interfaceTextBox.Text = interfaceValue));
            else
                interfaceTextBox.Text = interfaceValue;
        }

        private void UpdateMacTextBox(string macValue)
        {
            if (macTextBox.InvokeRequired)
                macTextBox.Invoke(new Action(() => macTextBox.Text = macValue));
            else
                macTextBox.Text = macValue;
        }

        private void UpdateUptimeTextBox(string uptimeValue)
        {
            if (uptimeTextBox.InvokeRequired)
                uptimeTextBox.Invoke(new Action(() => uptimeTextBox.Text = uptimeValue));
            else
                uptimeTextBox.Text = uptimeValue;
        }

        private void UpdateMacVendorLabel(string vendorValue)
        {
            if (MacVendorLbl.InvokeRequired)
                MacVendorLbl.Invoke(new Action(() => MacVendorLbl.Text = $"Vendor: {vendorValue}"));
            else
                MacVendorLbl.Text = $"Vendor: {vendorValue}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _viewModel.DisconnectAsync().GetAwaiter().GetResult();
            base.OnFormClosing(e);
        }
    }
}