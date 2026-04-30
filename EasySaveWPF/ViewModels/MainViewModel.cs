using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using EasySaveWPF.Models;
using EasySaveWPF.Services;

namespace EasySaveWPF.ViewModels
{
    // Main view model responsible for handling the primary UI logic, data binding, and user interactions
    public class MainViewModel : ViewModelBase
    {
        // Path to the local JSON file storing the configured backup jobs
        private readonly string _jobsFilePath = "jobs.json";

        // Observable collection bound to the DataGrid to display current backup jobs
        public ObservableCollection<BackupJob> Jobs { get; set; }

        // Collection of available languages for the application UI
        public ObservableCollection<string> AvailableLanguages { get; set; }

        private string _selectedLanguage;

        // Currently selected language. Updates UI bindings dynamically when changed
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();

                    // Notify the view to refresh all localized text properties
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(RunButtonText));
                    OnPropertyChanged(nameof(AddButtonText));
                    OnPropertyChanged(nameof(SettingsButtonText));
                    OnPropertyChanged(nameof(HeaderName));
                    OnPropertyChanged(nameof(HeaderSource));
                    OnPropertyChanged(nameof(HeaderTarget));
                    OnPropertyChanged(nameof(HeaderType));
                }
            }
        }

        private BackupJob _selectedJob;

        // Represents the currently selected job in the UI grid
        public BackupJob SelectedJob
        {
            get => _selectedJob;
            set { _selectedJob = value; OnPropertyChanged(); }
        }

        // Dynamic UI text properties returning localized strings based on the selected language
        public string WindowTitle => SelectedLanguage == "Français" ? "EasySave 2.0 - Gestionnaire" : "EasySave 2.0 - Manager";
        public string RunButtonText => SelectedLanguage == "Français" ? "▶ Lancer la sélection" : "▶ Run Selected";
        public string AddButtonText => SelectedLanguage == "Français" ? "➕ Nouveau Travail" : "➕ New Job";
        public string SettingsButtonText => SelectedLanguage == "Français" ? "⚙️ Paramètres" : "⚙️ Settings";

        // Localized headers for the DataGrid columns
        public string HeaderName => SelectedLanguage == "Français" ? "Nom" : "Name";
        public string HeaderSource => "Source";
        public string HeaderTarget => SelectedLanguage == "Français" ? "Cible" : "Target";
        public string HeaderType => "Type";

        // Commands bound to the main UI buttons
        public ICommand RunCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SettingsCommand { get; }

        // Initializes the view model, sets defaults, loads configurations, and binds commands
        public MainViewModel()
        {
            AvailableLanguages = new ObservableCollection<string> { "Français", "English" };
            _selectedLanguage = "Français";

            // Load application settings such as log format preferences at startup
            LoadSettingsAtStartup();

            // Initialize the job collection by reading the local storage file
            Jobs = new ObservableCollection<BackupJob>(LoadJobs());

            // Initialize command bindings
            RunCommand = new RelayCommand(ExecuteRun, CanExecuteRun);
            AddCommand = new RelayCommand(ExecuteAdd);
            SettingsCommand = new RelayCommand(ExecuteSettings);
        }

        // Reads the settings file to configure application-wide parameters
        private void LoadSettingsAtStartup()
        {
            string settingsFilePath = "settings.json";
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(settingsFilePath);
                    var settings = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json);

                    // Inject the user's preferred log format (JSON or XML) into the LoggerService
                    if (settings != null && settings.TryGetValue("LogFormat", out string? format))
                    {
                        EasyLog.LoggerService.Instance.LogFormat = format;
                    }
                }
                catch { } // Retain default JSON format if the settings file is corrupted or unreadable
            }
        }

        // Executes the selected backup jobs (handles multiple selection sequentially)
        private void ExecuteRun(object parameter)
        {
            // Casts the parameter received from the XAML into a list of items
            var selectedItems = parameter as System.Collections.IList;

            // Failsafe in case nothing is selected
            if (selectedItems == null || selectedItems.Count == 0) return;

            BackupService service = new BackupService();
            var allJobs = new System.Collections.Generic.List<BackupJob>(Jobs);
            int successCount = 0;

            try
            {
                // Loops through ALL selected jobs and executes them sequentially
                foreach (var item in selectedItems)
                {
                    if (item is BackupJob jobToRun)
                    {
                        service.ExecuteBackup(jobToRun, allJobs);
                        successCount++;
                    }
                }

                // Notifies the user once the entire queue is finished
                MessageBox.Show(
                    SelectedLanguage == "Français" ? $"{successCount} tâche(s) terminée(s) !" : $"{successCount} task(s) finished!",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                // Halts the sequence and warns the user if business software interrupts
                MessageBox.Show(ex.Message, "Attention / Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Determines whether the Run command can execute (requires a selected job)
        private bool CanExecuteRun(object parameter) => SelectedJob != null;

        // Opens the Add Job dialog and appends the new configuration to the collection upon success
        private void ExecuteAdd(object parameter)
        {
            var addWindow = new Views.AddJobWindow(SelectedLanguage);

            if (addWindow.ShowDialog() == true && addWindow.CreatedJob != null)
            {
                Jobs.Add(addWindow.CreatedJob);
                SaveJobs();
            }
        }

        // Opens the application settings dialog, passing the current language for localization
        private void ExecuteSettings(object parameter)
        {
            var settingsWindow = new Views.SettingsWindow(SelectedLanguage);
            settingsWindow.ShowDialog();
        }

        // Deserializes and loads backup jobs from the local JSON storage
        private System.Collections.Generic.List<BackupJob> LoadJobs()
        {
            if (!File.Exists(_jobsFilePath)) return new System.Collections.Generic.List<BackupJob>();
            string json = File.ReadAllText(_jobsFilePath);
            return JsonSerializer.Deserialize<System.Collections.Generic.List<BackupJob>>(json) ?? new System.Collections.Generic.List<BackupJob>();
        }

        // Serializes and saves the current collection of backup jobs to the local JSON storage
        private void SaveJobs()
        {
            string json = JsonSerializer.Serialize(Jobs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_jobsFilePath, json);
        }
    }
}