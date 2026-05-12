using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace EasySaveWPF.Views
{
    // Interaction logic and state management for the application configuration view.
    public partial class SettingsWindow : Window
    {
        // Relative path identifying the configuration persistence layer.
        private readonly string _settingsFilePath = "settings.json";

        // Initializes the view component and injects the active localization context.
        public SettingsWindow(string currentLanguage)
        {
            InitializeComponent();
            LoadSettings();

            // Override default presentation layer bindings based on the injected culture context.
            if (currentLanguage == "English")
            {
                Title = "Settings";
                LblLogFormat.Text = "Log Format (JSON or XML):";
                LblLogTarget.Text = "Logs Location:";
                LblBusinessSoftware.Text = "Blocking business software (e.g., notepad, CalculatorApp):";
                LblCryptoExtensions.Text = "Extensions to encrypt (separated by a comma, e.g.: .txt, .pdf):";

                LblPriorityExtensions.Text = "Priority extensions (separated by a comma, e.g.: .txt, .json):";
                LblMaxFileSize.Text = "Max file size for simultaneous transfer in KB (e.g., 50):";

                LblLogDestination.Text = "Log Destination (Local, Docker, Both):";

                BtnCancel.Content = "Cancel";
                BtnSave.Content = "Save";
            }
        }

        // Hydrates the view's data controls by deserializing the persistent configuration state.
        private void LoadSettings()
        {
            // Establish baseline configuration thresholds to guarantee system stability.
            CmbLogFormat.SelectedIndex = 0;
            CmbLogDestination.SelectedIndex = 0;
            TxtBusinessSoftware.Text = "notepad";
            TxtCryptoExtensions.Text = ".txt,.pdf";
            TxtPriorityExtensions.Text = ".txt";
            TxtMaxFileSize.Text = "50";

            // VALEURS PAR DÉFAUT DES NOUVEAUX CHAMPS
            TxtPriorityExtensions.Text = ".iso,.mp4";
            TxtMaxFileSize.Text = "52428800"; // 50 Mo par défaut

            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    // Reconstruct configuration mapping from the local filesystem.
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (settings != null)
                    {
                        // Map deserialized state to the corresponding UI components.
                        if (settings.TryGetValue("LogFormat", out string? format))
                            CmbLogFormat.SelectedIndex = format == "XML" ? 1 : 0;

                        if (settings.TryGetValue("BusinessSoftware", out string? software))
                            TxtBusinessSoftware.Text = software;

                        if (settings.TryGetValue("CryptoExtensions", out string? extensions))
                            TxtCryptoExtensions.Text = extensions;

                        if (settings.TryGetValue("PriorityExtensions", out string? prioExt))
                            TxtPriorityExtensions.Text = prioExt;

                        if (settings.TryGetValue("MaxFileSizeKb", out string? maxKb))
                            TxtMaxFileSize.Text = maxKb;

                        // Resolve telemetry routing parameters.
                        if (settings.TryGetValue("LogDestination", out string? dest))
                        {
                            if (dest == "Docker") CmbLogDestination.SelectedIndex = 1;
                            else if (dest == "Both") CmbLogDestination.SelectedIndex = 2;
                            else CmbLogDestination.SelectedIndex = 0;
                        }
                    }
                }
                catch
                {
                    // Swallow transient serialization or I/O faults to enforce graceful degradation via default values.
                }
            }
        }

        // Commits the modified configuration state to the persistence layer and triggers service re-initialization.
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Aggregate primitive data from presentation controls into a serializable data contract.
            var settings = new Dictionary<string, string>
            {
                { "LogFormat", CmbLogFormat.Text },
                { "LogTarget", CmbLogTarget.Text },
                { "BusinessSoftware", TxtBusinessSoftware.Text },
                { "CryptoExtensions", TxtCryptoExtensions.Text },
                { "PriorityExtensions", TxtPriorityExtensions.Text },
                { "MaxFileSizeKb", TxtMaxFileSize.Text },
                { "LogDestination", CmbLogDestination.Text }
            };

            // Overwrite local storage with the serialized representation of the updated context.
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);

            // Hot-reload critical configuration properties directly into active singleton instances.
            EasyLog.LoggerService.Instance.LogFormat = CmbLogFormat.Text;
            EasyLog.LoggerService.Instance.LogDestination = CmbLogDestination.Text;

            // Resolve the dialog interaction with a positive assertion.
            DialogResult = true;
            Close();
        }

        // Aborts the configuration context without committing pending mutations.
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}