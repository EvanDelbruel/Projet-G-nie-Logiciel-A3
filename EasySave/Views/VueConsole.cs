using System;

namespace EasySave.Views
{
    // affichage console
    public class VueConsole
    {
        // langue par defaut
        public string LangueActuelle { get; set; } = "FR";

        // choix de la langue au lancement
        public void ChoisirLangue()
        {
            Console.WriteLine("Choose your language / Choisissez votre langue :");
            Console.WriteLine("1. Français");
            Console.WriteLine("2. English");
            Console.Write("Choix / Choice (1-2) : ");

            string choix = Console.ReadLine();

            // check du choix
            if (choix == "2")
            {
                LangueActuelle = "EN";
            }
            else
            {
                LangueActuelle = "FR";
            }

            Console.Clear(); // clear console
        }

        // affiche le menu fr ou en
        public void AfficherMenuPrincipal()
        {
            if (LangueActuelle == "FR")
            {
                Console.WriteLine("\n=== MENU PRINCIPAL EASYSAVE ===");
                Console.WriteLine("1. Créer un travail de sauvegarde");
                Console.WriteLine("2. Afficher les travaux de sauvegarde");
                Console.WriteLine("3. Exécuter un travail de sauvegarde");
                Console.WriteLine("4. Quitter");
                Console.Write("Votre choix : ");
            }
            else
            {
                Console.WriteLine("\n=== EASYSAVE MAIN MENU ===");
                Console.WriteLine("1. Create a backup job");
                Console.WriteLine("2. Show backup jobs");
                Console.WriteLine("3. Execute a backup job");
                Console.WriteLine("4. Exit");
                Console.Write("Your choice : ");
            }
        }

        // recupere la saisie
        public string LireSaisieUtilisateur()
        {
            return Console.ReadLine();
        }

        // affiche le bon message selon la langue selectionnee
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