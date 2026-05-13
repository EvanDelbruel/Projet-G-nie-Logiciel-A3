using System.ComponentModel; // Essential for the UI

namespace EasySaveWPF.Models
{
    // DESIGN PATTERN: MVVM (Model Layer) & Observer Pattern
    // Implementing INotifyPropertyChanged is MANDATORY for WPF to update the progress bar in real-time
    public class BackupJob : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        private string _state = "Inactive";
        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        // The progress parameter
        private int _progression;
        public int Progression
        {
            get => _progression;
            set
            {
                if (_progression != value)
                {
                    _progression = value;
                    // Notifies the UI that the value has changed, triggering a visual update
                    OnPropertyChanged(nameof(Progression));
                }
            }
        }

        public BackupJob() { }

        public BackupJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Name} | Source: {SourceDirectory} | Target: {TargetDirectory} | Type: {Type}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}