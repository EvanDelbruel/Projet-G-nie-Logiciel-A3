using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;
using EasyLog;

namespace EasySave.ViewModels
{
    public class BackupViewModel
    {
        public List<BackupJob> BackupJobs { get; set; }
        public BackupState CurrentState { get; set; }
        public string Language { get; set; } = "EN"; // Par défaut

        private string saveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EasySave");
        private string stateFilePath;
        private string jobsFilePath;

        public BackupViewModel()
        {
            stateFilePath = Path.Combine(saveDir, "state.json");
            jobsFilePath = Path.Combine(saveDir, "jobs.json");

            BackupJobs = new List<BackupJob>();
            CurrentState = new BackupState();
            LoadJobs();
        }

        private void LoadJobs()
        {
            if (File.Exists(jobsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(jobsFilePath);
                    BackupJobs = JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();
                }
                catch { BackupJobs = new List<BackupJob>(); }
            }
        }

        private void SaveJobs()
        {
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
            File.WriteAllText(jobsFilePath, JsonSerializer.Serialize(BackupJobs, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void AddJob(string name, string source, string target, BackupType type)
        {
            if (BackupJobs.Count < 5)
            {
                BackupJobs.Add(new BackupJob { Name = name, SourceDirectory = source, TargetDirectory = target, Type = type });
                SaveJobs();
            }
        }

        public void ExecuteJob(int index)
        {
            if (index < 0 || index >= BackupJobs.Count) return;
            BackupJob job = BackupJobs[index];

            Console.WriteLine($"\n--- Lancement de la sauvegarde : {job.Name} ---");
            Console.WriteLine($"Source : {job.SourceDirectory}");
            Console.WriteLine($"Cible  : {job.TargetDirectory}");

            if (!Directory.Exists(job.SourceDirectory))
            {
                Console.WriteLine($"[ERREUR] Le dossier source n'existe pas ou le chemin est faux !");
                return;
            }

            if (!Directory.Exists(job.TargetDirectory))
            {
                Console.WriteLine("Création du dossier cible automatique...");
                Directory.CreateDirectory(job.TargetDirectory);
            }

            string[] allFiles = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            Console.WriteLine($"{allFiles.Length} fichier(s) trouvé(s) !");

            // --- LOGIQUE DIFFÉRENTIELLE ---
            List<string> filesToCopy = new List<string>();
            foreach (var f in allFiles)
            {
                if (job.Type == BackupType.Full)
                {
                    filesToCopy.Add(f);
                }
                else
                {
                    string relative = f.Substring(job.SourceDirectory.Length + 1);
                    string target = Path.Combine(job.TargetDirectory, relative);
                    if (!File.Exists(target) || File.GetLastWriteTime(f) > File.GetLastWriteTime(target))
                        filesToCopy.Add(f);
                }
            }

            Console.WriteLine($"{filesToCopy.Count} fichier(s) à copier (après vérification Différentielle).");

            CurrentState.Name = job.Name;
            CurrentState.State = StateStatus.Active;
            CurrentState.TotalFilesToCopy = filesToCopy.Count;
            CurrentState.NbFilesLeftToDo = filesToCopy.Count;
            CurrentState.Progression = 0;
            SaveState();

            foreach (string file in filesToCopy)
            {
                string relative = file.Substring(job.SourceDirectory.Length + 1);
                string targetPath = Path.Combine(job.TargetDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                CurrentState.CurrentSourceFilePath = file;
                CurrentState.CurrentTargetFilePath = targetPath;
                SaveState();

                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(file, targetPath, true);
                watch.Stop();

                LogManager.SaveLog(job.Name, file, targetPath, new FileInfo(file).Length, watch.Elapsed.TotalMilliseconds);

                CurrentState.NbFilesLeftToDo--;
                if (filesToCopy.Count > 0)
                    CurrentState.Progression = (int)((1.0 - (double)CurrentState.NbFilesLeftToDo / filesToCopy.Count) * 100);
                SaveState();


                Console.WriteLine($"Copié : {relative}");
            }

            CurrentState.State = StateStatus.Inactive;
            CurrentState.Progression = 100;
            SaveState();

            Console.WriteLine($"--- Sauvegarde terminée ! ---");
        }

        private void SaveState()
        {
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
            File.WriteAllText(stateFilePath, JsonSerializer.Serialize(CurrentState, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}