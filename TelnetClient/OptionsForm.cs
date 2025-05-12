using System;
using System.IO;
using System.Windows.Forms;

namespace TelnetClient
{
    public partial class OptionsForm : Form
    {
        private readonly Settings settings;
        private readonly Action<Settings> saveSettingsCallback;
        private readonly string logFilePath = "telnet_log.txt";

        // UI элементы
        private TextBox switchIpTextBox;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        private TextBox loginPromptTextBox;
        private TextBox passwordPromptTextBox;
        private Button saveButton;
        private Button cancelButton;

        public OptionsForm(Settings settings, Action<Settings> saveSettingsCallback)
        {
            this.settings = settings ?? new Settings();
            this.saveSettingsCallback = saveSettingsCallback;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Инициализация UI элементов
            switchIpTextBox = new TextBox { Location = new System.Drawing.Point(120, 10), Width = 200, Text = settings.SwitchIp };
            usernameTextBox = new TextBox { Location = new System.Drawing.Point(120, 40), Width = 200, Text = settings.Username };
            passwordTextBox = new TextBox { Location = new System.Drawing.Point(120, 70), Width = 200, Text = CryptoHelper.Decrypt(settings.Password), PasswordChar = '*' };
            loginPromptTextBox = new TextBox { Location = new System.Drawing.Point(120, 100), Width = 200, Text = settings.LoginPrompt };
            passwordPromptTextBox = new TextBox { Location = new System.Drawing.Point(120, 130), Width = 200, Text = settings.PasswordPrompt };
            saveButton = new Button { Text = "Save", Location = new System.Drawing.Point(120, 160), Width = 100 };
            cancelButton = new Button { Text = "Cancel", Location = new System.Drawing.Point(230, 160), Width = 100 };

            // Метки
            var switchIpLabel = new Label { Text = "Switch IP:", Location = new System.Drawing.Point(10, 10), Width = 100 };
            var usernameLabel = new Label { Text = "Username:", Location = new System.Drawing.Point(10, 40), Width = 100 };
            var passwordLabel = new Label { Text = "Password:", Location = new System.Drawing.Point(10, 70), Width = 100 };
            var loginPromptLabel = new Label { Text = "Login Prompt:", Location = new System.Drawing.Point(10, 100), Width = 100 };
            var passwordPromptLabel = new Label { Text = "Password Prompt:", Location = new System.Drawing.Point(10, 130), Width = 100 };

            // Добавление обработчиков событий
            saveButton.Click += SaveButton_Click;
            cancelButton.Click += (s, e) => this.Close();

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[] { switchIpTextBox, usernameTextBox, passwordTextBox,
                loginPromptTextBox, passwordPromptTextBox, saveButton, cancelButton,
                switchIpLabel, usernameLabel, passwordLabel, loginPromptLabel, passwordPromptLabel });

            // Настройки формы
            this.Text = "Options";
            this.Size = new System.Drawing.Size(350, 230);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(switchIpTextBox.Text) || string.IsNullOrEmpty(usernameTextBox.Text) ||
                string.IsNullOrEmpty(passwordTextBox.Text) || string.IsNullOrEmpty(loginPromptTextBox.Text) ||
                string.IsNullOrEmpty(passwordPromptTextBox.Text))
            {
                MessageBox.Show("All fields are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogToFile("Save failed: All fields are required");
                return;
            }

            try
            {
                settings.SwitchIp = switchIpTextBox.Text.Trim();
                settings.Username = usernameTextBox.Text.Trim();
                settings.Password = CryptoHelper.Encrypt(passwordTextBox.Text.Trim());
                settings.LoginPrompt = loginPromptTextBox.Text.Trim();
                settings.PasswordPrompt = passwordPromptTextBox.Text.Trim();

                LogToFile("Attempting to save settings via callback");
                saveSettingsCallback?.Invoke(settings);
                LogToFile("Settings saved successfully");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                LogToFile($"Save failed: {ex.Message}");
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LogToFile(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
            File.AppendAllText(logFilePath, logMessage);
        }
    }
}