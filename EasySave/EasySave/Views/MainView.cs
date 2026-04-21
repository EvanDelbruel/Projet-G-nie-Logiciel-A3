using System;
using EasySave.ViewModels;
using EasySave.Models;

namespace EasySave.Views
{
    public class MainView
    {
        private BackupViewModel viewModel;
        private bool isFrench = true;

        public MainView()
        {
            viewModel = new BackupViewModel();
        }

        public void ShowMenu()
        {
            // Choix de la langue au départ
            Console.WriteLine("Choose Language / Choisissez la langue :");
            Console.WriteLine("1. English\n2. Français");
            isFrench = Console.ReadLine() == "2";

            while (true)
            {
                Console.Clear();
                Console.WriteLine(isFrench ? "=== EASYSAVE V1.0 ===" : "=== EASYSAVE V1.0 ===");
                Console.WriteLine(isFrench ? "1. Créer un travail" : "1. Create a Backup Job");
                Console.WriteLine(isFrench ? "2. Exécuter un travail" : "2. Execute a Backup Job");
                Console.WriteLine(isFrench ? "3. Quitter" : "3. Exit");

                string choice = Console.ReadLine();
                if (choice == "1") ShowCreate();
                else if (choice == "2") ShowExecute();
                else if (choice == "3") break;
            }
        }

        private void ShowCreate()
        {
            Console.WriteLine(isFrench ? "Nom :" : "Name :");
            string n = Console.ReadLine();
            Console.WriteLine(isFrench ? "Source :" : "Source :");
            string s = Console.ReadLine();
            Console.WriteLine(isFrench ? "Cible :" : "Target :");
            string t = Console.ReadLine();
            Console.WriteLine(isFrench ? "Type (1: Complet, 2: Diff) :" : "Type (1: Full, 2: Diff) :");
            BackupType tp = Console.ReadLine() == "2" ? BackupType.Differential : BackupType.Full;

            viewModel.AddJob(n, s, t, tp);
        }

        private void ShowExecute()
        {
            for (int i = 0; i < viewModel.BackupJobs.Count; i++)
                Console.WriteLine($"{i + 1}. {viewModel.BackupJobs[i].Name}");

            Console.WriteLine(isFrench ? "Sélectionnez (ex: 1) :" : "Select (ex: 1) :");
            if (int.TryParse(Console.ReadLine(), out int idx))
                viewModel.ExecuteJob(idx - 1);

            Console.ReadLine();
        }

        // Utile pour la ligne de commande plus tard
        public BackupViewModel GetViewModel() => viewModel;
    }
}