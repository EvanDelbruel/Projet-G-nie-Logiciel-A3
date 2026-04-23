using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasyLog
{

    public sealed class LoggerService
    {
        // Static variables for the Singleton pattern and Thread-Safety
        private static LoggerService _instance = null;
        private static readonly object _padlock = new object();


        private readonly string _logDirectory = "Logs";
        private readonly string _stateFilePath;


        private LoggerService()
        {

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            _stateFilePath = Path.Combine(_logDirectory, "state.json");
        }


        public static LoggerService Instance
        {
            get
            {
                // The lock ensures that only one thread can create the instance
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


        public void WriteLog(LogModel logEntry)
        {
            string dailyLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.json");
            List<LogModel> dailyLogs = new List<LogModel>();


            if (File.Exists(dailyLogFile))
            {
                string existingContent = File.ReadAllText(dailyLogFile);
                if (!string.IsNullOrWhiteSpace(existingContent))
                {
                    try
                    {
                        dailyLogs = JsonSerializer.Deserialize<List<LogModel>>(existingContent) ?? new List<LogModel>();
                    }
                    catch { dailyLogs = new List<LogModel>(); }
                }
            }

            dailyLogs.Add(logEntry);


            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(dailyLogs, options);

            File.WriteAllText(dailyLogFile, jsonString);
        }


        public void WriteState(List<StateModel> allStates)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(allStates, options);

            File.WriteAllText(_stateFilePath, jsonString);
        }
    }
}