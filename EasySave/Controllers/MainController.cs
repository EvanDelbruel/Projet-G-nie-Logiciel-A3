using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;
using EasySave.Views;
using EasySave.Services;
using EasyLog;
namespace EasySave.Controllers
{
    public class MainController
    {
        private ConsoleView view;
        private List<BackupJob> jobList;
        private readonly string jobsFilePath = "jobs.json";
        private readonly int MAX_JOBS = 5;

        public MainController()
        {
            view = new ConsoleView();
            jobList = LoadJobs();
            // Explicitly trigger the LoggerService Singleton instance at startup.
            // This ensures the "Logs" directory and "state.json" are initialized immediately.
            _ = LoggerService.Instance;
        }

        public void Start(string[] args)
        {
            // If the program is launched with arguments (e.g., EasySave.exe 1-3)
            if (args != null && args.Length > 0)
            {
                string fullArgument = string.Join(";", args);
                ProcessCommandLineArguments(fullArgument);
                return; // Stop here to bypass the interactive menu
            }

            // Otherwise, launch the normal interactive menu
            view.ChooseLanguage();
            bool exitApp = false;

            while (!exitApp)
            {
                ShowMenu();
                string choice = view.GetUserInput();
                Console.Clear();

                switch (choice)
                {
                    case "1": CreateNewJob(); break;
                    case "2": ShowAllJobs(); break;
                    case "3": RunBackup(); break;
                    case "4": exitApp = true; break;
                    default: view.ShowMessage("Choix invalide.", "Invalid choice."); break;
                }
            }
        }

        private void ShowMenu()
        {
            if (view.CurrentLanguage == "FR")
            {
                Console.WriteLine("\n=== MENU PRINCIPAL EASYSAVE ===");
                Console.WriteLine("1. Créer un travail de sauvegarde");
                Console.WriteLine("2. Afficher les travaux de sauvegarde");
                Console.WriteLine("3. Lancer une sauvegarde");
                Console.WriteLine("4. Quitter");
            }
            else
            {
                Console.WriteLine("\n=== EASYSAVE MAIN MENU ===");
                Console.WriteLine("1. Create a backup job");
                Console.WriteLine("2. Show backup jobs");
                Console.WriteLine("3. Run a backup");
                Console.WriteLine("4. Exit");
            }
        }

        private void CreateNewJob()
        {
            // Check the 5 jobs limit
            if (jobList.Count >= MAX_JOBS)
            {
                view.ShowMessage($"Erreur : Limite de {MAX_JOBS} travaux atteinte.", $"Error: Limit of {MAX_JOBS} jobs reached.");
                return;
            }

            view.ShowMessage("\n=== NOUVEAU TRAVAIL ===", "\n=== NEW JOB ===");
            view.ShowMessage("Nom de la sauvegarde :", "Backup name:");
            string name = view.GetUserInput();

            view.ShowMessage("Dossier source (ex: C:\\Source) :", "Source folder (ex: C:\\Source):");
            string source = view.GetUserInput();

            view.ShowMessage("Dossier cible (ex: D:\\Cible) :", "Target folder (ex: D:\\Target):");
            string target = view.GetUserInput();

            view.ShowMessage("Type (Complete / Differential) :", "Type (Complete / Differential):");
            string type = view.GetUserInput();

            BackupJob newJob = new BackupJob(name, source, target, type);
            jobList.Add(newJob);
            SaveJobs();

            Console.Clear();
            view.ShowMessage("Travail sauvegardé avec succès !", "Job saved successfully!");
        }

        private void RunBackup()
        {
            if (jobList.Count == 0)
            {
                view.ShowMessage("Aucun travail configuré.", "No jobs configured.");
                return;
            }

            ShowAllJobs();
            view.ShowMessage("N° du travail (1-5) ou 'T' pour tous :", "Job number (1-5) or 'A' for all:");
            string input = view.GetUserInput().ToUpper();

            BackupService service = new BackupService();

            try
            {
                if (input == "T" || input == "A")
                {
                    foreach (var job in jobList) ExecuteAndShow(service, job);
                }
                else if (int.TryParse(input, out int idx) && idx > 0 && idx <= jobList.Count)
                {
                    ExecuteAndShow(service, jobList[idx - 1]);
                }
                else
                {
                    view.ShowMessage("Choix invalide.", "Invalid choice.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Erreur/Error] {ex.Message}");
                Console.ResetColor();
            }
        }

        private void ExecuteAndShow(BackupService service, BackupJob job)
        {
            view.ShowMessage($"\n[INFO] Démarrage de la tâche : {job.Name}", $"\n[INFO] Starting task: {job.Name}");
            
            // WE NOW PASS THE LIST TO THE SERVICE SO IT CAN BUILD THE STATE ARRAY
            service.ExecuteBackup(job, jobList); 
            
            view.ShowMessage($"[INFO] Tâche {job.Name} terminée.", $"[INFO] Task {job.Name} finished.");
        }

        // Translates console arguments into backup jobs to run
        private void ProcessCommandLineArguments(string argument)
        {
            BackupService service = new BackupService();
            List<int> jobsToRun = new List<int>();

            try
            {
                // Range (e.g., 1-3)
                if (argument.Contains('-'))
                {
                    string[] bounds = argument.Split('-');
                    int start = int.Parse(bounds[0]);
                    int end = int.Parse(bounds[1]);
                    for (int i = start; i <= end; i++) jobsToRun.Add(i);
                }
                // Separated (e.g., 1;3 or 1,3)
                else if (argument.Contains(';') || argument.Contains(','))
                {
                    string[] indices = argument.Split(new char[] { ';', ',' });
                    foreach (string i in indices) jobsToRun.Add(int.Parse(i));
                }
                // Single number (e.g., 2)
                else
                {
                    jobsToRun.Add(int.Parse(argument));
                }

                // Execute the targeted jobs
                foreach (int index in jobsToRun)
                {
                    if (index > 0 && index <= jobList.Count)
                    {
                        Console.WriteLine($"\n[Console] Starting job {index}...");
                        ExecuteAndShow(service, jobList[index - 1]);
                    }
                    else
                    {
                        Console.WriteLine($"[Console] Job {index} does not exist.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erreur/Error] Invalid arguments ({ex.Message}). Expected format: 1-3, 1;3 or 2.");
            }
        }

        private List<BackupJob> LoadJobs()
        {
            if (!File.Exists(jobsFilePath)) return new List<BackupJob>();
            string json = File.ReadAllText(jobsFilePath);
            return JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();
        }

        private void SaveJobs()
        {
            string json = JsonSerializer.Serialize(jobList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jobsFilePath, json);
        }

        private void ShowAllJobs()
        {
            for (int i = 0; i < jobList.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {jobList[i].ToString()}");
            }
        }
    }
}