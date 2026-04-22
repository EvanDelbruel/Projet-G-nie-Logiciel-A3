using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasyLog.Models;

namespace EasyLog.Services
{
    public class LoggerService
    {
        private readonly string logDirectory = "Logs";
        private readonly string stateFilePath = "state.json";

        public LoggerService()
        {
            // Ensures the Logs directory exists
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        // Writes a single entry to the daily log file
        public void WriteLog(LogModel logEntry)
        {
            string dailyLogFile = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");
            List<LogModel> logs = new List<LogModel>();

            if (File.Exists(dailyLogFile))
            {
                string existingContent = File.ReadAllText(dailyLogFile);
                if (!string.IsNullOrWhiteSpace(existingContent))
                {
                    logs = JsonSerializer.Deserialize<List<LogModel>>(existingContent);
                }
            }

            logs.Add(logEntry);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(dailyLogFile, JsonSerializer.Serialize(logs, options));
        }

        // Overwrites the state.json file with the array of all jobs
        public void WriteState(List<StateModel> states)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(stateFilePath, JsonSerializer.Serialize(states, options));
        }
    }
}