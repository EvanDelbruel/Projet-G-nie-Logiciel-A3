using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace EasySaveWPF.Views
{
    // Interaction logic and state management for the application configuration view.
    public partial class SettingsWindow : Window
    {
        // Relative path identifying the configuration persistence layer using strict AppData directory.
        private readonly string _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "settings.json");

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
                LblBusinessSoftware.Text = "Blocking business software (e.g., notepad, CalculatorApp):";
                LblCryptoExtensions.Text = "Extensions to encrypt (separated by a comma, e.g.: .txt, .pdf):";

                LblPriorityExtensions.Text = "Priority extensions (separated by a comma, e.g.: .txt, .json):";
                LblMaxFileSize.Text = "Max file size for simultaneous transfer in KB (e.g., 50):";

                LblLogDestination.Text = "Log Destination (Local, Docker, Both):";

                // Dynamic translation for the ComboBox content
                ((ComboBoxItem)CmbLogDestination.Items[2]).Content = "Both";

                BtnCancel.Content = "Cancel";
                BtnSave.Content = "Save";
            }
            else
            {
                // French fallback for the ComboBox content
                ((ComboBoxItem)CmbLogDestination.Items[2]).Content = "Les Deux";
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
                            else if (dest == "Both" || dest == "Les Deux") CmbLogDestination.SelectedIndex = 2;
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
                { "BusinessSoftware", TxtBusinessSoftware.Text },
                { "CryptoExtensions", TxtCryptoExtensions.Text },
                { "PriorityExtensions", TxtPriorityExtensions.Text },
                { "MaxFileSizeKb", TxtMaxFileSize.Text },
                { "LogDestination", CmbLogDestination.Text }
            };

            // Overwrite local storage with the serialized representation of the updated context.
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            string dir = Path.GetDirectoryName(_settingsFilePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_settingsFilePath, json);

            // Hot-reload critical configuration properties directly into active singleton instances.
            EasyLog.LoggerService.Instance.LogFormat = CmbLogFormat.Text;

            // Normalize "Les Deux" to "Both" for the backend if needed, but saving exactly what's on screen works too.
            string finalDest = CmbLogDestination.Text == "Les Deux" ? "Both" : CmbLogDestination.Text;
            EasyLog.LoggerService.Instance.LogDestination = finalDest;

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