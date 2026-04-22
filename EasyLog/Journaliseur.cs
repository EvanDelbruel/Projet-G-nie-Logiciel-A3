using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasyLog
{
    // structure pour le format json
    public class EntreeLog
    {
        public string Name { get; set; }
        public string FileSource { get; set; }
        public string FileTarget { get; set; }
        public long FileSize { get; set; }
        public double FileTransferTime { get; set; }
        public string time { get; set; }
    }

    // classe principale pour ecrire les logs
    public class Journaliseur
    {
        public void EcrireLog(string nomSauvegarde, string source, string cible, long taille, double tempsTransfertMs)
        {
            string dateDuJour = DateTime.Now.ToString("yyyy-MM-dd");
            string nomFichier = $"{dateDuJour}.json";

            // creation de la nouvelle ligne de log
            EntreeLog nouvelleEntree = new EntreeLog
            {
                Name = nomSauvegarde,
                FileSource = source,
                FileTarget = cible,
                FileSize = taille,
                FileTransferTime = tempsTransfertMs,
                time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            List<EntreeLog> listeLogs = new List<EntreeLog>();

            // recupere l'historique si il existe deja
            if (File.Exists(nomFichier))
            {
                string contenuExistant = File.ReadAllText(nomFichier);
                if (!string.IsNullOrWhiteSpace(contenuExistant))
                {
                    // conversion en liste d'objets
                    listeLogs = JsonSerializer.Deserialize<List<EntreeLog>>(contenuExistant);
                }
            }

            // on ajoute le nouveau log
            listeLogs.Add(nouvelleEntree);

            // ecriture du fichier avec indentation
            var options = new JsonSerializerOptions { WriteIndented = true };
            string texteJsonFinal = JsonSerializer.Serialize(listeLogs, options);
            File.WriteAllText(nomFichier, texteJsonFinal);
        }
    }
}