using System;

namespace EasySaveWPF.Models
{
    // Represents a backup task containing all necessary configuration details
    public class BackupJob
    {
        // Core properties defining the backup job parameters
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public string Type { get; set; }

        // Parameterless constructor strictly required for JSON serialization and deserialization
        public BackupJob() { }

        // Initializes a new backup job instance with specific configuration parameters
        public BackupJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }

        // Overrides the default string representation to provide a formatted output for UI and debugging
        public override string ToString()
        {
            return $"{Name} | Source: {SourceDirectory} | Target: {TargetDirectory} | Type: {Type}";
        }
    }
}