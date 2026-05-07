using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace EasySaveWPF.Views
{
    public partial class SettingsWindow : Window
    {
        // Path to the local JSON file storing the application configurations
        private readonly string _settingsFilePath = "settings.json";

        public SettingsWindow(string currentLanguage)
        {
            InitializeComponent();
            LoadSettings();

            // Applies dynamic UI localization based on the currently selected language
            if (currentLanguage == "English")
            {
                Title = "Settings";
                LblLogFormat.Text = "Log Format (JSON or XML):";
                LblLogTarget.Text = "Logs Location:";
                LblBusinessSoftware.Text = "Blocking business software (e.g., notepad, CalculatorApp):";
                LblCryptoExtensions.Text = "Extensions to encrypt (separated by a comma, e.g.: .txt, .pdf):";
                BtnCancel.Content = "Cancel";
                BtnSave.Content = "Save";
            }
        }

        private void LoadSettings()
        {
            // Establish default fallback values in case the configuration file is missing or unreadable
            CmbLogFormat.SelectedIndex = 0;
            CmbLogTarget.SelectedIndex = 2;
            TxtBusinessSoftware.Text = "notepad";
            TxtCryptoExtensions.Text = ".txt,.pdf";

            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (settings != null)
                    {
                        if (settings.TryGetValue("LogFormat", out string? f)) CmbLogFormat.Text = f;
                        if (settings.TryGetValue("LogTarget", out string? t)) CmbLogTarget.Text = t;
                        if (settings.TryGetValue("BusinessSoftware", out string? s)) TxtBusinessSoftware.Text = s;
                        if (settings.TryGetValue("CryptoExtensions", out string? e)) TxtCryptoExtensions.Text = e;
                    }
                }
                catch
                {
                    // Suppress exceptions to ensure default values are retained if the file is corrupted
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Collect the current UI input values into a dictionary mapping
            var settings = new Dictionary<string, string>
            {
                { "LogFormat", CmbLogFormat.Text },
                { "LogTarget", CmbLogTarget.Text },
                { "BusinessSoftware", TxtBusinessSoftware.Text },
                { "CryptoExtensions", TxtCryptoExtensions.Text }
            };

            // Serialize and save the updated configuration to the local JSON storage
            File.WriteAllText(_settingsFilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));

            // Immediately apply the new log format to the running instance of the logger service
            EasyLog.LoggerService.Instance.LogFormat = CmbLogFormat.Text;
            EasyLog.LoggerService.Instance.LogTarget = CmbLogTarget.Text;

            // Signal successful save operation and close the dialog
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Abort the configuration update and close the dialog
            DialogResult = false;
            Close();
        }
    }
}