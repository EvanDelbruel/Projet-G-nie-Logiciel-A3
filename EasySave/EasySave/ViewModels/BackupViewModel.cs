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
        public string Language { get; set; } = "EN";

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
                SaveState(); // On actualise le state.json pour montrer le nouveau job
            }
        }

        // --- LA MÉTHODE QUI MANQUAIT ---
        public void DeleteJob(int index)
        {
            if (index >= 0 && index < BackupJobs.Count)
            {
                string deletedName = BackupJobs[index].Name;
                BackupJobs.RemoveAt(index);

                SaveJobs();

                // Si on supprime le job qui était affiché comme actif, on réinitialise l'état
                if (CurrentState.Name == deletedName)
                {
                    CurrentState = new BackupState();
                }

                SaveState();
                Console.WriteLine($"[SUCCESS] Job '{deletedName}' deleted.");
            }
        }

        public void ExecuteJob(int index)
        {
            if (index < 0 || index >= BackupJobs.Count) return;
            BackupJob job = BackupJobs[index];

            Console.WriteLine($"\n--- Lancement : {job.Name} ---");

            if (!Directory.Exists(job.SourceDirectory))
            {
                Console.WriteLine($"[ERREUR] Source introuvable !");
                return;
            }

            if (!Directory.Exists(job.TargetDirectory)) Directory.CreateDirectory(job.TargetDirectory);

            // Calcul de la taille totale (Livrable 1)
            string[] allFiles = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            List<string> filesToCopy = new List<string>();
            long totalSize = 0;

            foreach (var f in allFiles)
            {
                string target = Path.Combine(job.TargetDirectory, f.Substring(job.SourceDirectory.Length + 1));
                if (job.Type == BackupType.Full || !File.Exists(target) || File.GetLastWriteTime(f) > File.GetLastWriteTime(target))
                {
                    filesToCopy.Add(f);
                    totalSize += new FileInfo(f).Length;
                }
            }

            // Initialisation de l'état
            CurrentState.Name = job.Name;
            CurrentState.State = StateStatus.Active;
            CurrentState.TotalFilesToCopy = filesToCopy.Count;
            CurrentState.TotalFilesSize = totalSize;
            CurrentState.NbFilesLeftToDo = filesToCopy.Count;
            CurrentState.RemainingFilesSize = totalSize;
            CurrentState.Progression = 0;
            SaveState();

            foreach (string file in filesToCopy)
            {
                long fileSize = new FileInfo(file).Length;
                string targetPath = Path.Combine(job.TargetDirectory, file.Substring(job.SourceDirectory.Length + 1));

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                CurrentState.CurrentSourceFilePath = file;
                CurrentState.CurrentTargetFilePath = targetPath;
                SaveState();

                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(file, targetPath, true);
                watch.Stop();

                LogManager.SaveLog(job.Name, file, targetPath, fileSize, watch.Elapsed.TotalMilliseconds);

                // Mise à jour temps réel (Livrable 1)
                CurrentState.NbFilesLeftToDo--;
                CurrentState.RemainingFilesSize -= fileSize;
                if (totalSize > 0)
                    CurrentState.Progression = (int)((double)(totalSize - CurrentState.RemainingFilesSize) / totalSize * 100);

                SaveState();
                Console.WriteLine($"Copié : {Path.GetFileName(file)}");
            }

            CurrentState.State = StateStatus.Inactive;
            CurrentState.Progression = 100;
            CurrentState.RemainingFilesSize = 0;
            SaveState();
            Console.WriteLine($"--- Terminé ! ---");
        }

        private void SaveState()
        {
            try
            {
                if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

                List<BackupState> allStates = new List<BackupState>();
                foreach (var job in BackupJobs)
                {
                    if (job.Name == CurrentState.Name) allStates.Add(CurrentState);
                    else allStates.Add(new BackupState { Name = job.Name });
                }

                File.WriteAllText(stateFilePath, JsonSerializer.Serialize(allStates, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { Console.WriteLine("Error saving state: " + ex.Message); }
        }
    }
}