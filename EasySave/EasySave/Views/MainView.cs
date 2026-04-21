using System;
using System.Collections.Generic;
using EasySave.Models;

namespace EasySave.Views
{
    public class ConsoleView
    {
        public bool IsFrench { get; private set; } = true;

        // Choix de la langue au démarrage
        public void ChooseLanguage()
        {
            Console.WriteLine("Choose Language / Choisissez la langue :");
            Console.WriteLine("1. English\n2. Français");
            IsFrench = Console.ReadLine() == "2";
        }

        // Affichage du menu principal
        public string ShowMainMenu()
        {
            Console.Clear();
            Console.WriteLine("=== EASYSAVE V1.0 (MVC) ===");
            Console.WriteLine(IsFrench ? "1. Créer un travail" : "1. Create a Backup Job");
            Console.WriteLine(IsFrench ? "2. Exécuter un travail" : "2. Execute a Backup Job");
            Console.WriteLine(IsFrench ? "3. Supprimer un travail" : "3. Delete a Backup Job");
            Console.WriteLine(IsFrench ? "4. Quitter" : "4. Exit");
            return Console.ReadLine();
        }

        // Outil générique pour poser une question et récupérer du texte
        public string AskForString(string messageFr, string messageEn)
        {
            Console.WriteLine(IsFrench ? messageFr : messageEn);
            return Console.ReadLine();
        }

        // Outil générique pour afficher un message
        public void DisplayMessage(string messageFr, string messageEn)
        {
            Console.WriteLine(IsFrench ? messageFr : messageEn);
        }

        // Affiche la liste des travaux
        public void ShowJobsList(List<BackupJob> jobs)
        {
            if (jobs.Count == 0)
            {
                Console.WriteLine(IsFrench ? "Liste vide." : "Empty list.");
                return;
            }
            for (int i = 0; i < jobs.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {jobs[i].Name}");
            }
        }

        // Outil pour mettre le programme en pause
        public void WaitForKey()
        {
            Console.WriteLine(IsFrench ? "\nAppuyez sur Entrée pour continuer..." : "\nPress Enter to continue...");
            Console.ReadLine();
        }
    }
}