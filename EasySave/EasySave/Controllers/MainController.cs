using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;
using EasySave.Views;
using EasyLog;
using System.Text.Json.Serialization;

namespace EasySave.Controllers
{
    public class MainController
    {
        private List<BackupJob> BackupJobs;
        private BackupState CurrentState;
        private ConsoleView view; // Le contrôleur possède la vue

        private string saveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EasySave");
        private string stateFilePath;
        private string jobsFilePath;

        public MainController()
        {
            view = new ConsoleView();
            stateFilePath = Path.Combine(saveDir, "state.json");
            jobsFilePath = Path.Combine(saveDir, "jobs.json");
            BackupJobs = new List<BackupJob>();
            CurrentState = new BackupState();
            LoadJobs();
        }

        // --- POINT D'ENTRÉE PRINCIPAL ---
        public void Start(string[] args)
        {
            // Mode Ligne de Commande (CLI)
            if (args.Length > 0)
            {
                List<int> indexes = ParseSelection(args[0]);
                foreach (int idx in indexes) ExecuteJob(idx);
                Console.WriteLine("Command Line Execution Finished.");
                return;
            }

            // Mode Interactif
            view.ChooseLanguage();
            while (true)
            {
                string choice = view.ShowMainMenu();
                if (choice == "1") CreateJob();
                else if (choice == "2") ExecuteSelectedJobs();
                else if (choice == "3") DeleteSelectedJob();
                else if (choice == "4") break;
            }
        }

        // --- GESTION DES MENUS ---
        private void CreateJob()
        {
            if (BackupJobs.Count >= 5)
            {
                string answer = view.AskForString(
                    "Limite de 5 travaux atteinte. Voulez-vous en supprimer un ? (O/N) :",
                    "Limit of 5 jobs reached. Do you want to delete one? (Y/N) :");

                if (answer.ToUpper() == "O" || answer.ToUpper() == "Y")
                {
                    DeleteSelectedJob(); // Redirige vers la suppression
                }
                return;
            }

            string name = view.AskForString("Nom :", "Name :");
            string source = view.AskForString("Source :", "Source :");
            string target = view.AskForString("Cible :", "Target :");
            string typeStr = view.AskForString("Type (1: Complet, 2: Diff) :", "Type (1: Full, 2: Diff) :");
            BackupType type = (typeStr == "2") ? BackupType.Differential : BackupType.Full;

            BackupJobs.Add(new BackupJob { Name = name, SourceDirectory = source, TargetDirectory = target, Type = type });
            SaveJobs();

            // On initialise un état propre pour le nouveau job
            CurrentState = new BackupState { Name = name };
            SaveState();

            view.DisplayMessage("Travail créé !", "Job created!");
            view.WaitForKey();
        }

        private void ExecuteSelectedJobs()
        {
            view.ShowJobsList(BackupJobs);
            if (BackupJobs.Count == 0) { view.WaitForKey(); return; }

            string input = view.AskForString("Sélectionnez (ex: 1, 1-3) :", "Select (ex: 1, 1-3) :");
            List<int> indexes = ParseSelection(input);

            foreach (int idx in indexes) ExecuteJob(idx);
            view.WaitForKey();
        }

        private void DeleteSelectedJob()
        {
            view.ShowJobsList(BackupJobs);
            if (BackupJobs.Count == 0) { view.WaitForKey(); return; }

            string input = view.AskForString("\nNuméro à supprimer :", "\nNumber to delete :");
            if (int.TryParse(input, out int idx) && idx > 0 && idx <= BackupJobs.Count)
            {
                string deletedName = BackupJobs[idx - 1].Name;
                BackupJobs.RemoveAt(idx - 1);
                SaveJobs();
                if (CurrentState.Name == deletedName) CurrentState = new BackupState();
                SaveState();
                view.DisplayMessage($"Job '{deletedName}' supprimé.", $"Job '{deletedName}' deleted.");
            }
            view.WaitForKey();
        }

        // --- LOGIQUE MÉTIER ---
        private void ExecuteJob(int index)
        {
            if (index < 0 || index >= BackupJobs.Count) return;
            BackupJob job = BackupJobs[index];

            view.DisplayMessage($"\n--- Lancement : {job.Name} ---", $"\n--- Launching : {job.Name} ---");

            if (!Directory.Exists(job.SourceDirectory))
            {
                view.DisplayMessage("[ERREUR] Source introuvable !", "[ERROR] Source not found !");
                return;
            }

            if (!Directory.Exists(job.TargetDirectory)) Directory.CreateDirectory(job.TargetDirectory);

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

                // CORRECTION ERREUR ACCES REFUSÉ (Fichiers en lecture seule comme .git)
                if (File.Exists(targetPath))
                {
                    File.SetAttributes(targetPath, FileAttributes.Normal);
                }

                File.Copy(file, targetPath, true);
                watch.Stop();

                LogManager.SaveLog(job.Name, file, targetPath, fileSize, watch.Elapsed.TotalMilliseconds);

                CurrentState.NbFilesLeftToDo--;
                CurrentState.RemainingFilesSize -= fileSize;
                if (totalSize > 0)
                    CurrentState.Progression = (int)((double)(totalSize - CurrentState.RemainingFilesSize) / totalSize * 100);

                SaveState();
                view.DisplayMessage($"Copié : {Path.GetFileName(file)}", $"Copied : {Path.GetFileName(file)}");
            }

            CurrentState.State = StateStatus.Inactive;
            CurrentState.Progression = 100;
            CurrentState.RemainingFilesSize = 0;
            SaveState();
            view.DisplayMessage("--- Terminé ! ---", "--- Finished ! ---");
        }

        // --- OUTILS INTERNES ---
        private List<int> ParseSelection(string input)
        {
            List<int> indexes = new List<int>();
            try
            {
                if (input.Contains("-"))
                {
                    string[] p = input.Split('-');
                    for (int i = int.Parse(p[0]); i <= int.Parse(p[1]); i++) indexes.Add(i - 1);
                }
                else if (input.Contains(";"))
                {
                    foreach (var s in input.Split(';')) indexes.Add(int.Parse(s) - 1);
                }
                else { if (int.TryParse(input, out int idx)) indexes.Add(idx - 1); }
            }
            catch { view.DisplayMessage("Format invalide.", "Invalid format."); }
            return indexes;
        }

        private void LoadJobs()
        {
            if (File.Exists(jobsFilePath))
            {
                try { BackupJobs = JsonSerializer.Deserialize<List<BackupJob>>(File.ReadAllText(jobsFilePath)) ?? new List<BackupJob>(); }
                catch { BackupJobs = new List<BackupJob>(); }
            }
        }

        private void SaveJobs()
        {
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

            // Ajout du convertisseur pour écrire "Full" au lieu de "0" dans jobs.json
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());

            File.WriteAllText(jobsFilePath, JsonSerializer.Serialize(BackupJobs, options));
        }

        private void SaveState()
        {
            try
            {
                if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());

                List<BackupState> allStates = new List<BackupState>();
                if (File.Exists(stateFilePath))
                {
                    try
                    {
                        allStates = JsonSerializer.Deserialize<List<BackupState>>(File.ReadAllText(stateFilePath), options) ?? new List<BackupState>();
                    }
                    catch { }
                }

                var existingState = allStates.Find(s => s.Name == CurrentState.Name);
                if (existingState != null)
                {
                    int index = allStates.IndexOf(existingState);

                    allStates[index] = new BackupState
                    {
                        Name = CurrentState.Name,
                        Timestamp = CurrentState.Timestamp,
                        State = CurrentState.State,
                        TotalFilesToCopy = CurrentState.TotalFilesToCopy,
                        TotalFilesSize = CurrentState.TotalFilesSize,
                        NbFilesLeftToDo = CurrentState.NbFilesLeftToDo,
                        Progression = CurrentState.Progression,
                        RemainingFilesSize = CurrentState.RemainingFilesSize,
                        CurrentSourceFilePath = CurrentState.CurrentSourceFilePath,
                        CurrentTargetFilePath = CurrentState.CurrentTargetFilePath
                    };
                }
                else if (!string.IsNullOrEmpty(CurrentState.Name))
                {
                    allStates.Add(CurrentState);
                }

                //  On ajoute les autres travaux
                foreach (var job in BackupJobs)
                {
                    if (!allStates.Exists(s => s.Name == job.Name))
                    {
                        allStates.Add(new BackupState { Name = job.Name });
                    }
                }

                // Nettoyage
                allStates.RemoveAll(s => !BackupJobs.Exists(j => j.Name == s.Name));

                // Sauvegarde
                File.WriteAllText(stateFilePath, JsonSerializer.Serialize(allStates, options));
            }
            catch { }
        }
    }
}