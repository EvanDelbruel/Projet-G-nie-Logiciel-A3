using System;
using EasySave.ViewModels;
using EasySave.Models;

namespace EasySave.Views
{
    public class MainView
    {
        // La vue possède le ViewModel pour lui donner des ordres
        private BackupViewModel viewModel;

        public MainView()
        {
            viewModel = new BackupViewModel();
        }

        // Boucle principale du menu
        public void ShowMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== EASYSAVE V1.0 ===");
                Console.WriteLine("1. Create a Backup Job");
                Console.WriteLine("2. Execute a Backup Job");
                Console.WriteLine("3. Exit");
                Console.Write("\nChoose an option: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        CreateJobView();
                        break;
                    case "2":
                        ExecuteJobView();
                        break;
                    case "3":
                        return; // Quitte le programme
                    default:
                        Console.WriteLine("Invalid choice. Press Enter to try again.");
                        Console.ReadLine();
                        break;
                }
            }
        }

        // L'écran pour créer une sauvegarde
        private void CreateJobView()
        {
            Console.Clear();
            Console.WriteLine("--- CREATE BACKUP JOB ---");

            Console.Write("Name: ");
            string name = Console.ReadLine();

            Console.Write("Source Directory: ");
            string source = Console.ReadLine();

            Console.Write("Target Directory: ");
            string target = Console.ReadLine();

            Console.Write("Type (1: Full, 2: Differential): ");
            string typeChoice = Console.ReadLine();
            BackupType type = (typeChoice == "2") ? BackupType.Differential : BackupType.Full;

            // On envoie les infos au ViewModel pour qu'il enregistre !
            viewModel.AddJob(name, source, target, type);

            Console.WriteLine("\nPress Enter to return to menu.");
            Console.ReadLine();
        }

        // L'écran pour lancer une sauvegarde
        private void ExecuteJobView()
        {
            Console.Clear();
            Console.WriteLine("--- EXECUTE BACKUP JOB ---");

            if (viewModel.BackupJobs.Count == 0)
            {
                Console.WriteLine("No jobs available. Please create one first.");
            }
            else
            {
                // On affiche la liste des sauvegardes
                for (int i = 0; i < viewModel.BackupJobs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {viewModel.BackupJobs[i].Name}");
                }

                Console.Write("\nSelect a job number to execute: ");
                if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= viewModel.BackupJobs.Count)
                {
                    // On demande au ViewModel de lancer la sauvegarde choisie
                    viewModel.ExecuteJob(index - 1);
                }
                else
                {
                    Console.WriteLine("Invalid selection.");
                }
            }

            Console.WriteLine("\nPress Enter to return to menu.");
            Console.ReadLine();
        }
    }
}