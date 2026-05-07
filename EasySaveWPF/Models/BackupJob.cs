using System;
using EasySaveWPF.ViewModels; // Ajout du namespace pour ViewModelBase

namespace EasySaveWPF.Models
{
    public class BackupJob : ViewModelBase // Héritage ajouté ici
    {
        private string _name = "";
        private string _sourceDirectory = "";
        private string _targetDirectory = "";
        private string _type = "";
        private string _state = "Inactive";
        private int _progression = 0;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string SourceDirectory { get => _sourceDirectory; set { _sourceDirectory = value; OnPropertyChanged(); } }
        public string TargetDirectory { get => _targetDirectory; set { _targetDirectory = value; OnPropertyChanged(); } }
        public string Type { get => _type; set { _type = value; OnPropertyChanged(); } }

        // Indispensable pour voir "Active" ou "Paused" à l'écran
        public string State { get => _state; set { _state = value; OnPropertyChanged(); } }

        // Indispensable pour voir la barre de progression bouger
        public int Progression { get => _progression; set { _progression = value; OnPropertyChanged(); } }

        public BackupJob() { }
        public BackupJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }
    }
}