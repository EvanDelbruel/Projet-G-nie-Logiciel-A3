using System;
using System.IO;
using System.Text.Json;

namespace EasyLog
{
    public class LogEntry
    {
        public string Timestamp { get; set; }
        public string BackupName { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public long FileSize { get; set; }
        public double TransferTimeMs { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public static class LogManager
    {
        // On définit le chemin du dossier "EasySave" dans les Documents
        private static string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EasySave");

        // On définit le chemin du fichier complet
        private static string logFilePath = Path.Combine(logDirectory, "logs.json");

        public static void SaveLog(string name, string source, string target, long size, double time)
        {
            var entry = new LogEntry
            {
                BackupName = name,
                SourcePath = source,
                TargetPath = target,
                FileSize = size,
                TransferTimeMs = time
            };

            try
            {
                // On vérifie si le dossier "EasySave" existe, sinon on le crée
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string jsonString = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
                File.AppendAllText(logFilePath, jsonString + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while writing log: " + ex.Message);
            }
        }
    }
}