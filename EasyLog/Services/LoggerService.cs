using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasyLog
{
    // Singleton service responsible for handling application logs and real-time state tracking.
    // Strictly uses JSON format as per Livrable 1.0 requirements.
    public sealed class LoggerService
    {
        // Static variables for the Singleton pattern implementation and thread synchronization
        private static LoggerService _instance = null;
        private static readonly object _padlock = new object();

        private readonly string _logDirectory = "Logs";
        private readonly string _stateFilePath;

        // Private constructor to prevent direct instantiation
        private LoggerService()
        {
            // Ensure the base log directory exists upon initialization
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _stateFilePath = Path.Combine(_logDirectory, "state.json");
        }

        // Gets the single, thread-safe instance of the LoggerService.
        public static LoggerService Instance
        {
            get
            {
                // The lock ensures thread safety so only one thread can instantiate the service at a time
                lock (_padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new LoggerService();
                    }
                    return _instance;
                }
            }
        }

        // Appends a new log entry to the daily JSON log file.
        public void WriteLog(LogModel logEntry)
        {
            List<LogModel> dailyLogs = new List<LogModel>();
            string dailyLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.json");

            if (File.Exists(dailyLogFile))
            {
                string existingContent = File.ReadAllText(dailyLogFile);
                if (!string.IsNullOrWhiteSpace(existingContent))
                {
                    try
                    {
                        // Read and deserialize the existing JSON file content
                        dailyLogs = JsonSerializer.Deserialize<List<LogModel>>(existingContent) ?? new List<LogModel>();
                    }
                    catch
                    {
                        // Initialize a new list if deserialization fails
                        dailyLogs = new List<LogModel>();
                    }
                }
            }

            dailyLogs.Add(logEntry);

            // Serialize and overwrite the updated list in indented JSON format
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(dailyLogs, options);

            File.WriteAllText(dailyLogFile, jsonString);
        }

        // Updates the real-time state file with the current progress of all tracked jobs.
        public void WriteState(List<StateModel> allStates)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(allStates, options);

            File.WriteAllText(_stateFilePath, jsonString);
        }
    }
}