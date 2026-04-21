using System;
using System.Collections.Generic;
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
            Console.WriteLine("Choose Language / Choisissez la langue :");
            Console.WriteLine("1. English\n2. Français");
            isFrench = Console.ReadLine() == "2";

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== EASYSAVE V1.0 ===");
                Console.WriteLine(isFrench ? "1. Créer un travail" : "1. Create a Backup Job");
                Console.WriteLine(isFrench ? "2. Exécuter un travail" : "2. Execute a Backup Job");
                Console.WriteLine(isFrench ? "3. Supprimer un travail" : "3. Delete a Backup Job");
                Console.WriteLine(isFrench ? "4. Quitter" : "4. Exit");

                string choice = Console.ReadLine();
                if (choice == "1") ShowCreate();
                else if (choice == "2") ShowExecute();
                else if (choice == "3") ShowDelete();
                else if (choice == "4") break;
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
            if (viewModel.BackupJobs.Count == 0) return;
            for (int i = 0; i < viewModel.BackupJobs.Count; i++)
                Console.WriteLine($"{i + 1}. {viewModel.BackupJobs[i].Name}");

            Console.WriteLine(isFrench ? "Sélectionnez (ex: 1, 1-3) :" : "Select (ex: 1, 1-3) :");
            string input = Console.ReadLine();
            List<int> indexes = ParseSelection(input);

            foreach (int idx in indexes) viewModel.ExecuteJob(idx);
            Console.ReadLine();
        }

        private void ShowDelete()
        {
            Console.Clear();
            if (viewModel.BackupJobs.Count == 0) { Console.WriteLine("Empty."); return; }

            for (int i = 0; i < viewModel.BackupJobs.Count; i++)
                Console.WriteLine($"{i + 1}. {viewModel.BackupJobs[i].Name}");

            Console.Write(isFrench ? "\nNuméro à supprimer : " : "\nNumber to delete: ");
            if (int.TryParse(Console.ReadLine(), out int idx) && idx > 0 && idx <= viewModel.BackupJobs.Count)
            {
                viewModel.DeleteJob(idx - 1);
            }
            Console.ReadLine();
        }

        public List<int> ParseSelection(string input)
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
            catch { }
            return indexes;
        }

        public BackupViewModel GetViewModel() => viewModel;
    }
}