using System;

namespace EasySave.Models
{
    // Cette classe représente un travail de sauvegarde (le Modèle)
    public class BackupJob
    {
        // Propriétés (Encapsulation basique)
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public string Type { get; set; } // "Complete" ou "Differentielle"

        // Constructeur pour initialiser un travail de sauvegarde facilement
        public BackupJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }

        // On redéfinit ToString() pour afficher facilement les infos, comme tu as appris à le faire
        public override string ToString()
        {
            return $"[{Name}] Type: {Type} | Source: {SourceDirectory} => Cible: {TargetDirectory}";
        }
    }
}