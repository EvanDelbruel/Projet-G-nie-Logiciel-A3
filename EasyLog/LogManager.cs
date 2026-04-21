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
 
        private static string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EasySave");

        public static void SaveLog(string name, string source, string target, long size, double time)
        {

            string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
            string logFilePath = Path.Combine(logDirectory, $"{todayDate}.json");

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