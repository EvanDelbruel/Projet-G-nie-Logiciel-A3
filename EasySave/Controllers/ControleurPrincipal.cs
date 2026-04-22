using System;
using System.Collections.Generic;
using EasySave.Models; // Pour pouvoir utiliser notre classe BackupJob
using EasySave.Views;  // Pour pouvoir utiliser notre classe VueConsole
using System.IO; // Pour manipuler les dossiers et fichiers
using System.Diagnostics; // Pour utiliser le chronomètre (Stopwatch)
using EasyLog; // Pour utiliser notre fameux Journaliseur !
using System.Text.Json;

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
            ChargerConfigTravaux(); // NOUVEAU : On charge la mémoire au démarrage !
        }

        // fct de demarrage avec prise en charge des arguments terminaux
        public void Demarrer(string[] args = null)
        {
            // === MODE LIGNE DE COMMANDE (Sans menu) ===
            if (args != null && args.Length > 0)
            {
                // ex: args[0] est "1-3" ou "1;3"
                string argument = args[0];
                vue.LangueActuelle = "EN"; // on force la langue pour la console

                Console.WriteLine($"[Mode Terminal Activé - Execution de : {argument}]\n");

                if (argument.Contains("-"))
                {
                    // cas 1-3
                    string[] bornes = argument.Split('-');
                    if (bornes.Length == 2 && int.TryParse(bornes[0], out int debut) && int.TryParse(bornes[1], out int fin))
                    {
                        for (int i = debut; i <= fin; i++)
                        {
                            if (i > 0 && i <= listeTravaux.Count) LancerCopie(listeTravaux[i - 1]);
                        }
                    }
                }
                else if (argument.Contains(";"))
                {
                    // cas 1;3
                    string[] cibles = argument.Split(';');
                    foreach (string c in cibles)
                    {
                        if (int.TryParse(c, out int index) && index > 0 && index <= listeTravaux.Count)
                        {
                            LancerCopie(listeTravaux[index - 1]);
                        }
                    }
                }

                return; // FIN ! On quitte le programme sans ouvrir le menu
            }

            // === MODE NORMAL (Avec Menu) ===
            vue.ChoisirLangue();

            bool quitter = false;
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
                        ExecuterTravail();
                        break;
                    case "4":
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

            // Tant que la source est vide, on le bloque et on lui redemande
            while (string.IsNullOrWhiteSpace(source))
            {
                vue.AfficherMessage("Erreur : La source ne peut pas être vide. Veuillez réessayer :",
                                    "Error: Source cannot be empty. Please try again:");
                source = vue.LireSaisieUtilisateur();
            }

            vue.AfficherMessage("Entrez le dossier cible (ex: D:\\DossierB) :", "Enter target folder (ex: D:\\FolderB):");
            string cible = vue.LireSaisieUtilisateur();

            // Tant que la cible est vide, on le bloque et on lui redemande !
            while (string.IsNullOrWhiteSpace(cible))
            {
                vue.AfficherMessage("Erreur : La cible ne peut pas être vide. Veuillez réessayer :",
                                    "Error: Target cannot be empty. Please try again:");
                cible = vue.LireSaisieUtilisateur();
            }

            vue.AfficherMessage("Entrez le type (Complet ou Differentiel) :", "Enter type (Complete or Differential):");
            string type = vue.LireSaisieUtilisateur();

            // On crée le "moule" (l'objet Modèle) avec les infos tapées
            BackupJob nouveauTravail = new BackupJob(nom, source, cible, type);

            // On l'ajoute à notre liste
            listeTravaux.Add(nouveauTravail);
            SauvegarderConfigTravaux(); // NOUVEAU : On sauvegarde la mémoire !

            Console.Clear();
            vue.AfficherMessage("Le travail a été créé avec succès !", "The job was successfully created!");

            // LA PAUSE
            Console.WriteLine();
            vue.AfficherMessage("Appuyez sur Entrée pour revenir au menu...", "Press Enter to return to the menu...");
            Console.ReadLine();
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

        // Méthode pour demander l'exécution des travaux depuis le menu
        private void ExecuterTravail()
        {
            if (listeTravaux.Count == 0)
            {
                vue.AfficherMessage("Erreur : Aucun travail à exécuter.", "Error: No job to execute.");
                return;
            }

            AfficherTousLesTravaux();
            vue.AfficherMessage("Entrez le numéro du travail (ou tapez 'Tous' / 'All' pour tout lancer) :",
                                "Enter the job number (or type 'All' to run everything):");

            string saisie = vue.LireSaisieUtilisateur().ToLower();

            if (saisie == "tous" || saisie == "all")
            {
                // exécution séquentielle de tous les travaux
                foreach (BackupJob travail in listeTravaux)
                {
                    LancerCopie(travail);
                }
                Console.WriteLine();
                vue.AfficherMessage("Toutes les sauvegardes sont terminées avec succès !", "All backups are completed successfully!");
            }
            else if (int.TryParse(saisie, out int index) && index > 0 && index <= listeTravaux.Count)
            {
                // exécution d'un seul travail
                BackupJob travailChoisi = listeTravaux[index - 1];
                LancerCopie(travailChoisi);

                Console.WriteLine();
                vue.AfficherMessage("Sauvegarde terminée avec succès !", "Backup completed successfully!");
            }
            else
            {
                vue.AfficherMessage("Saisie invalide.", "Invalid input.");
            }

            // LA PAUSE
            Console.WriteLine();
            vue.AfficherMessage("Appuyez sur Entrée pour revenir au menu...", "Press Enter to return to the menu...");
            Console.ReadLine();
        }

        // methode outil qui fait la vraie copie
        private void LancerCopie(BackupJob travailChoisi)
        {
            vue.AfficherMessage($"\nLancement de : {travailChoisi.Name} (Type: {travailChoisi.Type})...",
                                $"Starting: {travailChoisi.Name} (Type: {travailChoisi.Type})...");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[DEBUG] Chemin Source lu : '{travailChoisi.SourceDirectory}'");
            Console.WriteLine($"[DEBUG] Chemin Cible lu  : '{travailChoisi.TargetDirectory}'");
            Console.ResetColor();

            if (!Directory.Exists(travailChoisi.SourceDirectory))
            {
                vue.AfficherMessage("Erreur : Le dossier source n'existe pas.", "Error: Source folder does not exist.");
                return;
            }

            if (!Directory.Exists(travailChoisi.TargetDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[DEBUG] Le dossier cible n'existe pas, création en cours...");
                Console.ResetColor();
                Directory.CreateDirectory(travailChoisi.TargetDirectory);
            }

            Journaliseur monJournaliseur = new Journaliseur();

            // recupere les fichiers meme dans les sous-dossiers
            string[] tousLesFichiers = Directory.GetFiles(travailChoisi.SourceDirectory, "*", SearchOption.AllDirectories);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[DEBUG] Nombre de fichiers trouvés dans la source : {tousLesFichiers.Length}");
            Console.ResetColor();

            List<string> fichiersACopier = new List<string>();

            foreach (string f in tousLesFichiers)
            {
                bool fautIlCopier = true;

                if (travailChoisi.Type.ToLower().Contains("diff"))
                {
                    string cheminRelatifTest = Path.GetRelativePath(travailChoisi.SourceDirectory, f);
                    string cheminCibleTest = Path.Combine(travailChoisi.TargetDirectory, cheminRelatifTest);

                    if (File.Exists(cheminCibleTest))
                    {
                        FileInfo infoSource = new FileInfo(f);
                        FileInfo infoCible = new FileInfo(cheminCibleTest);

                        if (infoSource.LastWriteTime <= infoCible.LastWriteTime)
                        {
                            fautIlCopier = false;
                        }
                    }
                }

                if (fautIlCopier)
                {
                    fichiersACopier.Add(f);
                }
            }

            int totalFichiers = fichiersACopier.Count;
            long tailleTotale = 0;

            if (totalFichiers == 0)
            {
                vue.AfficherMessage("Aucun nouveau fichier à copier pour ce travail.", "No new files to copy for this job.");
                EcrireFichierEtat(); // on s'assure de remettre à END
                return;
            }

            foreach (string f in fichiersACopier)
            {
                tailleTotale += new FileInfo(f).Length;
            }

            int fichiersRestants = totalFichiers;
            long tailleRestante = tailleTotale;

            // boucle de copie
            foreach (string cheminFichierSource in fichiersACopier)
            {
                string cheminRelatif = Path.GetRelativePath(travailChoisi.SourceDirectory, cheminFichierSource);
                string cheminFichierCible = Path.Combine(travailChoisi.TargetDirectory, cheminRelatif);

                string dossierCibleDuFichier = Path.GetDirectoryName(cheminFichierCible);
                if (!Directory.Exists(dossierCibleDuFichier))
                {
                    Directory.CreateDirectory(dossierCibleDuFichier);
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[DEBUG] Je copie : {cheminFichierSource}");
                Console.WriteLine($"[DEBUG] Vers     : {cheminFichierCible}");
                Console.ResetColor();

                FileInfo infoFichier = new FileInfo(cheminFichierSource);
                long tailleFichier = infoFichier.Length;

                int progression = (int)(((totalFichiers - fichiersRestants) / (double)totalFichiers) * 100);

                EcrireFichierEtat(travailChoisi, cheminFichierSource, cheminFichierCible, totalFichiers, tailleTotale, fichiersRestants, tailleRestante, progression);

                Stopwatch chrono = new Stopwatch();
                chrono.Start();
                File.Copy(cheminFichierSource, cheminFichierCible, true);
                chrono.Stop();

                monJournaliseur.EcrireLog(travailChoisi.Name, cheminFichierSource, cheminFichierCible, tailleFichier, chrono.Elapsed.TotalMilliseconds);

                fichiersRestants--;
                tailleRestante -= tailleFichier;
            }

            EcrireFichierEtat(); // remet le state a END
        }

        // Méthode pour mettre à jour le fichier state.json en temps réel
        private void EcrireFichierEtat(BackupJob travailActif = null, string fichierSourceEnCours = "", string fichierCibleEnCours = "", int totalFichiers = 0, long tailleTotale = 0, int restants = 0, long tailleRestante = 0, int progression = 0)
        {
            List<EtatTravail> listeEtats = new List<EtatTravail>();
            string tempsActuel = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // on boucle sur les travaux
            foreach (BackupJob travail in listeTravaux)
            {
                EtatTravail etat = new EtatTravail();
                etat.Name = travail.Name;

                //  si c le bon travail on met a jour
                if (travailActif != null && travail.Name == travailActif.Name)
                {
                    etat.State = "ACTIVE";
                    etat.SourceFilePath = fichierSourceEnCours;
                    etat.TargetFilePath = fichierCibleEnCours;
                    etat.TotalFilesToCopy = totalFichiers;
                    etat.TotalFilesSize = tailleTotale;
                    etat.NbFilesLeftToDo = restants;
                    etat.SizeFilesLeftToDo = tailleRestante;
                    etat.LastActionTime = tempsActuel;
                    etat.Progression = progression;
                }
                else
                {
                    // sinon on reset a END 
                    etat.State = "END";
                    etat.SourceFilePath = "";
                    etat.TargetFilePath = "";
                    etat.TotalFilesToCopy = 0;
                    etat.TotalFilesSize = 0;
                    etat.NbFilesLeftToDo = 0;
                    etat.SizeFilesLeftToDo = 0;
                    etat.LastActionTime = tempsActuel;
                    etat.Progression = 0;
                }

                listeEtats.Add(etat);
            }

            // ecriture json
            var options = new JsonSerializerOptions { WriteIndented = true };
            string texteJson = JsonSerializer.Serialize(listeEtats, options);
            File.WriteAllText("state.json", texteJson);
        }

        // sauvegarde la config des travaux
        private void SauvegarderConfigTravaux()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(listeTravaux, options);
            File.WriteAllText("config_jobs.json", json);
        }

        // charge la config au demarrage
        private void ChargerConfigTravaux()
        {
            if (File.Exists("config_jobs.json"))
            {
                string json = File.ReadAllText("config_jobs.json");
                // on recharge notre liste
                listeTravaux = JsonSerializer.Deserialize<List<BackupJob>>(json);
            }
        }
    }
}