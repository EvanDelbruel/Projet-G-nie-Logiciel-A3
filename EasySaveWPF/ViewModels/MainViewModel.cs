using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using EasySaveWPF.Models;
using EasySaveWPF.Services;

namespace EasySaveWPF.ViewModels
{
    // Coordinates presentation logic, state management, and command routing for the primary user interface.
    public class MainViewModel : ViewModelBase
    {
        // Absolute or relative path defining the persistence layer for application state.
        private readonly string _jobsFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "EasySave", "jobs.json");

        // Data-bound collection representing the persisted state of configured backup routines.
        public ObservableCollection<BackupJob> Jobs { get; set; }

        // Defines the supported localization cultures for the application instance.
        public ObservableCollection<string> AvailableLanguages { get; set; }

        private string _selectedLanguage;

        // Manages localization state. Triggers the WPF binding engine to refresh localized resources upon mutation.
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();

                    // Force UI culture refresh by notifying the view of property state invalidations.
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(RunButtonText));
                    OnPropertyChanged(nameof(AddButtonText));
                    OnPropertyChanged(nameof(SettingsButtonText));
                    OnPropertyChanged(nameof(HeaderName));
                    OnPropertyChanged(nameof(HeaderSource));
                    OnPropertyChanged(nameof(HeaderTarget));
                    OnPropertyChanged(nameof(HeaderType));
                    OnPropertyChanged(nameof(HeaderActions));
                    OnPropertyChanged(nameof(ActionPlay));
                    OnPropertyChanged(nameof(ActionPause));
                    OnPropertyChanged(nameof(ActionStop));
                    OnPropertyChanged(nameof(PauseButtonText));
                    OnPropertyChanged(nameof(StopButtonText));
                }
            }
        }

        private BackupJob _selectedJob;

        // Maintains the selection state of the UI data grid for targeted command execution.
        public BackupJob SelectedJob
        {
            get => _selectedJob;
            set { _selectedJob = value; OnPropertyChanged(); }
        }

        // --- Computed Localization Properties ---
        public string WindowTitle => SelectedLanguage == "Français" ? "EasySave 3.0 - Gestionnaire" : "EasySave 3.0 - Manager";
        public string RunButtonText => SelectedLanguage == "Français" ? "▶ Lancer la sélection" : "▶ Run Selected";
        public string PauseButtonText => SelectedLanguage == "Français" ? "⏸ Pause (Sélection)" : "⏸ Pause Selected";
        public string StopButtonText => SelectedLanguage == "Français" ? "⏹ Stop (Sélection)" : "⏹ Stop Selected";
        public string AddButtonText => SelectedLanguage == "Français" ? "➕ Nouveau Travail" : "➕ New Job";
        public string SettingsButtonText => SelectedLanguage == "Français" ? "⚙️ Paramètres" : "⚙️ Settings";

        public string HeaderName => SelectedLanguage == "Français" ? "Nom" : "Name";
        public string HeaderSource => "Source";
        public string HeaderTarget => SelectedLanguage == "Français" ? "Cible" : "Target";
        public string HeaderType => "Type";
        public string HeaderActions => "Actions";
        
        public string ActionPlay => SelectedLanguage == "Français" ? "Lancer" : "Play";
        public string ActionPause => "Pause";
        public string ActionStop => "Stop";

        // --- Command Infrastructure ---
        // DESIGN PATTERN: Command Pattern
        // The Command Pattern is used here to encapsulate a request as an object (ICommand).
        // This decouples the UI (View) from the business logic (ViewModel), making it easier
        // to manage actions like Play, Pause, and Stop, and supporting the MVVM architecture.
        public ICommand RunCommand { get; }
        public ICommand PauseSelectedCommand { get; }
        public ICommand StopSelectedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SettingsCommand { get; }

        // Granular process control commands for individual background routines.
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand StopCommand { get; }

        // (Removed _isRunning to allow per-job execution state tracking)

        // Bootstraps the ViewModel, injecting default state, loading persistent data, and mapping ICommands.
        public MainViewModel()
        {
            AvailableLanguages = new ObservableCollection<string> { "Français", "English" };
            _selectedLanguage = "Français";

            LoadSettingsAtStartup();

            // Hydrate the observable collection from local storage.
            Jobs = new ObservableCollection<BackupJob>(LoadJobs());

            // Bind UI actions to local event handlers with execution validation where applicable.
            RunCommand = new RelayCommand(ExecuteRun, CanExecuteRun);
            PauseSelectedCommand = new RelayCommand(ExecutePauseSelected, CanExecutePauseSelected);
            StopSelectedCommand = new RelayCommand(ExecuteStopSelected, CanExecuteStopSelected);
            AddCommand = new RelayCommand(ExecuteAdd);
            SettingsCommand = new RelayCommand(ExecuteSettings);

            PauseCommand = new RelayCommand(ExecutePause);
            ResumeCommand = new RelayCommand(ExecuteResume);
            StopCommand = new RelayCommand(ExecuteStop);
        }

        // Resolves configuration parameters into memory to initialize singleton services.
        private void LoadSettingsAtStartup()
        {
            string settingsFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "EasySave", "settings.json");
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (settings != null)
                    {
                        // Inject telemetry formatting parameters.
                        if (settings.TryGetValue("LogFormat", out string? format))
                        {
                            EasyLog.LoggerService.Instance.LogFormat = format;
                        }

                        // Inject network routing configurations for remote telemetry.
                        if (settings.TryGetValue("LogDestination", out string? dest))
                        {
                            EasyLog.LoggerService.Instance.LogDestination = dest;
                        }
                    }
                }
                catch { } // Swallow transient I/O or parsing exceptions to ensure application resilience.
            }
        }

        // Asynchronously dispatches batch backup operations utilizing concurrent task execution.
        private async void ExecuteRun(object parameter)
        {
            if (parameter is System.Collections.IList selectedItems)
            {
                var jobsList = selectedItems.Cast<BackupJob>().ToList();
                var runningTasks = new List<Task>();
                var allJobs = new List<BackupJob>(Jobs);

                foreach (var job in jobsList)
                {
                    try
                    {
                        if (job.State == "Inactive" || job.State == "Stopped" || string.IsNullOrEmpty(job.State))
                        {
                            job.State = "Starting"; // Prevent double execution
                            BackupService service = new BackupService();
                            runningTasks.Add(service.ExecuteBackupAsync(job, allJobs));
                        }
                        else if (job.State == "Paused" || job.State == "Paused (Software)" || job.State == "Active")
                        {
                            new BackupService().ResumeJob(job.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        job.State = "Inactive";
                        MessageBox.Show(ex.Message, "Erreur / Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                if (runningTasks.Count > 0)
                {
                    await Task.WhenAll(runningTasks);
                    MessageBox.Show(
                        SelectedLanguage == "Français" ? $"Exécution terminée pour les tâches sélectionnées !" : $"Execution finished for selected tasks!",
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // --- Thread Lifecycle Management Proxies ---

        // Triggers the ManualResetEvent associated with the target thread to suspend execution.
        private void ExecutePause(object parameter)
        {
            if (parameter is BackupJob job)
                new BackupService().PauseJob(job.Name);
        }

        // Signals the ManualResetEvent to unblock thread execution.
        private async void ExecuteResume(object parameter)
        {
            if (parameter is BackupJob job)
            {
                try
                {
                    if (job.State == "Inactive" || job.State == "Stopped" || string.IsNullOrEmpty(job.State))
                    {
                        // Start the job if it is currently inactive
                        job.State = "Starting"; // Prevent double execution
                        var allJobs = new List<BackupJob>(Jobs);
                        BackupService service = new BackupService();
                        await service.ExecuteBackupAsync(job, allJobs);
                        
                        MessageBox.Show(
                            SelectedLanguage == "Français" ? $"Exécution terminée pour la tâche : {job.Name} !" : $"Execution finished for task: {job.Name}!",
                            "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Resume the job if it is currently paused
                        new BackupService().ResumeJob(job.Name);
                    }
                }
                catch (Exception ex)
                {
                    job.State = "Inactive";
                    MessageBox.Show(ex.Message, "Erreur / Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Invokes the CancellationToken to gracefully terminate the asynchronous operation.
        private void ExecuteStop(object parameter)
        {
            if (parameter is BackupJob job)
                new BackupService().StopJob(job.Name);
        }

        // Validates the application state against execution prerequisites to enable/disable UI controls.
        private bool CanExecuteRun(object parameter)
        {
            if (parameter is System.Collections.IList selectedItems && selectedItems.Count > 0)
            {
                return selectedItems.Cast<BackupJob>().Any(j => j.State != "Active" && j.State != "Starting");
            }
            return false;
        }

        private bool CanExecutePauseSelected(object parameter)
        {
            if (parameter is System.Collections.IList selectedItems && selectedItems.Count > 0)
            {
                return selectedItems.Cast<BackupJob>().Any(j => j.State == "Active");
            }
            return false;
        }

        private bool CanExecuteStopSelected(object parameter)
        {
            if (parameter is System.Collections.IList selectedItems && selectedItems.Count > 0)
            {
                return selectedItems.Cast<BackupJob>().Any(j => j.State == "Active" || j.State == "Paused" || j.State == "Paused (Software)");
            }
            return false;
        }

        private void ExecutePauseSelected(object parameter)
        {
            if (parameter is System.Collections.IList selectedItems)
            {
                var jobsToPause = selectedItems.Cast<BackupJob>().ToList();
                foreach (var job in jobsToPause) ExecutePause(job);
            }
        }

        private void ExecuteStopSelected(object parameter)
        {
            if (parameter is System.Collections.IList selectedItems)
            {
                var jobsToStop = selectedItems.Cast<BackupJob>().ToList();
                foreach (var job in jobsToStop) ExecuteStop(job);
            }
        }

        // Instantiates the job creation view and handles data aggregation upon successful dialog resolution.
        private void ExecuteAdd(object parameter)
        {
            var addWindow = new Views.AddJobWindow(SelectedLanguage);

            if (addWindow.ShowDialog() == true && addWindow.CreatedJob != null)
            {
                Jobs.Add(addWindow.CreatedJob);
                SaveJobs();
            }
        }

        // Dispatches the configuration context to a dedicated modal view.
        private void ExecuteSettings(object parameter)
        {
            var settingsWindow = new Views.SettingsWindow(SelectedLanguage);
            settingsWindow.ShowDialog();
        }

        // Reconstructs the internal state mapping by deserializing the persistence file.
        private List<BackupJob> LoadJobs()
        {
            if (!File.Exists(_jobsFilePath)) return new List<BackupJob>();
            string json = File.ReadAllText(_jobsFilePath);
            var loadedJobs = JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();
            
            // Clean up Zombie Jobs (in case app was closed while running)
            foreach (var job in loadedJobs)
            {
                job.State = "Inactive";
                job.Progression = 0;
            }
            
            return loadedJobs;
        }

        // Commits the current memory state to the local filesystem for persistence across sessions.
        private void SaveJobs()
        {
            string json = JsonSerializer.Serialize(Jobs, new JsonSerializerOptions { WriteIndented = true });
            string dir = Path.GetDirectoryName(_jobsFilePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_jobsFilePath, json);
        }
    }
}