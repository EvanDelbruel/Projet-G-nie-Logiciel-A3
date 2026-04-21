using System;
using System.Collections.Generic;
using System.IO;
using EasySave.Models;
using EasyLog;

namespace EasySave.ViewModels
{
    public class BackupViewModel
    {
        // La liste qui va contenir les 5 travaux de sauvegarde maximum
        public List<BackupJob> BackupJobs { get; set; }

        // Le modèle qui gère l'état en temps réel
        public BackupState CurrentState { get; set; }

        public BackupViewModel()
        {
            // Initialisation des listes et de l'état quand le programme démarre
            BackupJobs = new List<BackupJob>();
            CurrentState = new BackupState();
        }

        // Méthode pour ajouter un nouveau travail de sauvegarde à notre liste
        public void AddJob(string name, string source, string target, BackupType type)
        {
            if (BackupJobs.Count < 5)
            {
                var newJob = new BackupJob
                {
                    Name = name,
                    SourceDirectory = source,
                    TargetDirectory = target,
                    Type = type
                };
                BackupJobs.Add(newJob);
                Console.WriteLine($"[SUCCESS] Job '{name}' added successfully !");
            }
            else
            {
                Console.WriteLine("[ERROR] You can only create up to 5 backup jobs.");
            }
        }


        // Le  moteur de copie de fichiers
        public void ExecuteJob(int index)
        {
            // On vérifie que l'index demandé existe bien dans la liste
            if (index >= 0 && index < BackupJobs.Count)
            {
                BackupJob job = BackupJobs[index];
                Console.WriteLine($"\n--- Starting Backup : {job.Name} ---");
                Console.WriteLine($"Source: {job.SourceDirectory}");
                Console.WriteLine($"Target: {job.TargetDirectory}");

                //  Vérif si le dossier source existe vraiment
                if (!Directory.Exists(job.SourceDirectory))
                {
                    Console.WriteLine("[ERROR] The source directory does not exist.");
                    return;
                }

                // Créer le dossier cible s'il n'existe pas
                if (!Directory.Exists(job.TargetDirectory))
                {
                    Directory.CreateDirectory(job.TargetDirectory);
                }

                //  Récupérer tous les fichiers du dossier source
                string[] files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
                Console.WriteLine($"Found {files.Length} files to copy...");

                // Boucle de copie pour chaque fichier
                foreach (string file in files)
                {
                    // Calculer le chemin exact où le fichier doit aller
                    string relativePath = file.Substring(job.SourceDirectory.Length + 1);
                    string targetFilePath = Path.Combine(job.TargetDirectory, relativePath);
                    string targetDirPath = Path.GetDirectoryName(targetFilePath);

                    // Créer le sous-dossier cible s'il manque
                    if (!Directory.Exists(targetDirPath))
                    {
                        Directory.CreateDirectory(targetDirPath);
                    }

                    // Récupérer la taille du fichier pour le Log
                    FileInfo fileInfo = new FileInfo(file);
                    long fileSize = fileInfo.Length;

                    // Lancer le chronomètre
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    try
                    {

                        File.Copy(file, targetFilePath, true);
                        watch.Stop();

                        // Envoyer les infos à ta DLL EasyLog
                        LogManager.SaveLog(job.Name, file, targetFilePath, fileSize, watch.Elapsed.TotalMilliseconds);

                        Console.WriteLine($"Copied: {relativePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to copy {relativePath}: {ex.Message}");
                    }
                }

                Console.WriteLine($"--- Backup {job.Name} Finished ! ---\n");
            }
            else
            {
                Console.WriteLine("[ERROR] Backup job not found at this index.");
            }
        }
    }
}