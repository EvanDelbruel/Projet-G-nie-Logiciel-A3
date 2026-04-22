using System;

namespace EasySave.Models
{
    // classe pour stocker les infos d'une sauvegarde
    public class BackupJob
    {
        // les données du job
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public string Type { get; set; } // Complet ou Différentiel

        // le constructeur
        public BackupJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }

        // affiche les infos sous forme de texte
        public override string ToString()
        {
            return $"[{Name}] Type: {Type} | Source: {SourceDirectory} => Cible: {TargetDirectory}";
        }
    }
}