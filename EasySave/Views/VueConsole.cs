using System;

namespace EasySave.Views
{
    // Cette classe gère tout ce qui s'affiche à l'écran (La Vue)
    public class VueConsole
    {
        // On stocke la langue choisie par l'utilisateur (FR par défaut)
        public string LangueActuelle { get; set; } = "FR";

        // Méthode pour demander la langue au tout début
        public void ChoisirLangue()
        {
            Console.WriteLine("Choose your language / Choisissez votre langue :");
            Console.WriteLine("1. Français");
            Console.WriteLine("2. English");
            Console.Write("Choix / Choice (1-2) : ");

            string choix = Console.ReadLine();

            // Si l'utilisateur tape 2, on passe en anglais. Sinon on reste en français.
            if (choix == "2")
            {
                LangueActuelle = "EN";
            }
            else
            {
                LangueActuelle = "FR";
            }

            // On nettoie la console pour que ce soit propre
            Console.Clear();
        }

        // Méthode pour afficher le menu principal selon la langue
        public void AfficherMenuPrincipal()
        {
            if (LangueActuelle == "FR")
            {
                Console.WriteLine("\n=== MENU PRINCIPAL EASYSAVE ===");
                Console.WriteLine("1. Créer un travail de sauvegarde");
                Console.WriteLine("2. Afficher les travaux de sauvegarde");
                Console.WriteLine("3. Quitter");
                Console.Write("Votre choix : ");
            }
            else
            {
                Console.WriteLine("\n=== EASYSAVE MAIN MENU ===");
                Console.WriteLine("1. Create a backup job");
                Console.WriteLine("2. Show backup jobs");
                Console.WriteLine("3. Exit");
                Console.Write("Your choice : ");
            }
        }

        // Méthode simple pour lire ce que l'utilisateur tape
        public string LireSaisieUtilisateur()
        {
            return Console.ReadLine();
        }

        // Méthode pratique pour afficher un message d'erreur ou de succès dans la bonne langue
        public void AfficherMessage(string messageFr, string messageEn)
        {
            if (LangueActuelle == "FR")
            {
                Console.WriteLine(messageFr);
            }
            else
            {
                Console.WriteLine(messageEn);
            }
        }
    }
}