using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization; // For writing in XML

namespace EasyLog
{
    public sealed class LoggerService
    {
        // Static variables for the Singleton pattern and Thread-Safety
        private static LoggerService _instance = null;
        private static readonly object _padlock = new object();

        private readonly string _logDirectory = "Logs";
        private readonly string _stateFilePath;

        // Stores the chosen log format. Default is "JSON" to avoid breaking
        public string LogFormat { get; set; } = "JSON";

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
            List<LogModel> dailyLogs = new List<LogModel>();

            // --- IF THE USER CHOSE XML FORMAT ---
            if (LogFormat.ToUpper() == "XML")
            {
                string dailyLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.xml");

                if (File.Exists(dailyLogFile))
                {
                    try
                    {
                        // Read the existing XML file
                        XmlSerializer deserializer = new XmlSerializer(typeof(List<LogModel>));
                        using (TextReader reader = new StreamReader(dailyLogFile))
                        {
                            dailyLogs = (List<LogModel>)deserializer.Deserialize(reader);
                        }
                    }
                    catch { dailyLogs = new List<LogModel>(); }
                }

                dailyLogs.Add(logEntry);

                // Write the new list in XML format
                XmlSerializer serializer = new XmlSerializer(typeof(List<LogModel>));
                using (TextWriter writer = new StreamWriter(dailyLogFile))
                {
                    serializer.Serialize(writer, dailyLogs);
                }
            }
            // --- OTHERWISE: KEEP THE ORIGINAL JSON BEHAVIOR ---
            else
            {
                string dailyLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.json");

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
        }

        // The state file remains in JSON (standard format for real-time tracking)
        public void WriteState(List<StateModel> allStates)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(allStates, options);

            File.WriteAllText(_stateFilePath, jsonString);
        }
    }
}