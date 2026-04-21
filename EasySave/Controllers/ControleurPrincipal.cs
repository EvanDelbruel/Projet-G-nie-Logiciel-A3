using System;
using System.Collections.Generic;
using EasySave.Models; // Pour pouvoir utiliser notre classe BackupJob
using EasySave.Views;  // Pour pouvoir utiliser notre classe VueConsole

namespace EasySave.Controllers
{
    // C'est le cerveau de l'application (Le Contrôleur)
    public class ControleurPrincipal
    {
        // Le contrôleur a besoin de la vue pour l'affichage et d'une liste pour stocker les travaux
        private VueConsole vue;
        private List<BackupJob> listeTravaux;

        // Constructeur
        public ControleurPrincipal()
        {
            vue = new VueConsole();
            listeTravaux = new List<BackupJob>();
        }

        // Méthode principale qui lance l'application (comme la boucle while de ton exercice Bibliothèque)
        public void Demarrer()
        {
            // 1. On demande la langue en premier
            vue.ChoisirLangue();

            bool quitter = false;

            // 2. Boucle infinie tant que l'utilisateur ne choisit pas de quitter
            while (!quitter)
            {
                vue.AfficherMenuPrincipal();
                string choix = vue.LireSaisieUtilisateur();

                // On nettoie la console après le choix pour garder un écran propre
                Console.Clear();

                switch (choix)
                {
                    case "1":
                        CreerNouveauTravail();
                        break;
                    case "2":
                        AfficherTousLesTravaux();
                        break;
                    case "3":
                        quitter = true;
                        vue.AfficherMessage("Fermeture de EasySave... Au revoir !", "Closing EasySave... Goodbye!");
                        break;
                    default:
                        vue.AfficherMessage("Choix invalide. Veuillez réessayer.", "Invalid choice. Please try again.");
                        break;
                }
            }
        }

        // Méthode pour créer un travail de sauvegarde
        private void CreerNouveauTravail()
        {
            // Vérification de la contrainte des 5 travaux maximum
            if (listeTravaux.Count >= 5)
            {
                vue.AfficherMessage("Erreur : Vous avez atteint la limite de 5 travaux de sauvegarde.",
                                    "Error: You have reached the limit of 5 backup jobs.");
                return; // On arrête la méthode ici
            }

            vue.AfficherMessage("=== NOUVEAU TRAVAIL ===", "=== NEW JOB ===");

            vue.AfficherMessage("Entrez le nom de la sauvegarde :", "Enter backup name:");
            string nom = vue.LireSaisieUtilisateur();

            vue.AfficherMessage("Entrez le dossier source (ex: C:\\DossierA) :", "Enter source folder (ex: C:\\FolderA):");
            string source = vue.LireSaisieUtilisateur();

            vue.AfficherMessage("Entrez le dossier cible (ex: D:\\DossierB) :", "Enter target folder (ex: D:\\FolderB):");
            string cible = vue.LireSaisieUtilisateur();

            vue.AfficherMessage("Entrez le type (Complet ou Differentiel) :", "Enter type (Complete or Differential):");
            string type = vue.LireSaisieUtilisateur();

            // On crée le "moule" (l'objet Modèle) avec les infos tapées
            BackupJob nouveauTravail = new BackupJob(nom, source, cible, type);

            // On l'ajoute à notre liste
            listeTravaux.Add(nouveauTravail);

            Console.Clear();
            vue.AfficherMessage("Le travail a été créé avec succès !", "The job was successfully created!");
        }

        // Méthode pour afficher la liste des travaux
        private void AfficherTousLesTravaux()
        {
            vue.AfficherMessage("=== LISTE DES TRAVAUX ===", "=== LIST OF JOBS ===");

            if (listeTravaux.Count == 0)
            {
                vue.AfficherMessage("Aucun travail configuré pour le moment.", "No job configured yet.");
            }
            else
            {
                // On parcourt la liste comme dans l'exercice de la Bibliothèque
                for (int i = 0; i < listeTravaux.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {listeTravaux[i].ToString()}");
                }
            }

            Console.WriteLine(); // Ligne vide pour aérer
        }
    }
}