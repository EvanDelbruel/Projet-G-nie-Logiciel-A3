using System;

namespace EasySave.Models
{
    public class BackupJob
    {
        // Properties of the backup job
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public string Type { get; set; }

        // Empty constructor required for JSON deserialization
        public BackupJob() { }

        // Constructor used when creating a new job via the menu
        public BackupJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }

        // Formats the job info for console display
        public override string ToString()
        {
            return $"{Name} | Source: {SourceDirectory} | Target: {TargetDirectory} | Type: {Type}";
        }
    }
}