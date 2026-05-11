using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Net.Sockets;
using System.Text;

namespace EasyLog
{
    // Singleton service responsible for handling application logs and real-time state tracking.
    // Supports both JSON and XML file formats for daily logs.
    public sealed class LoggerService
    {
        // Static variables for the Singleton pattern implementation and thread synchronization
        private static LoggerService _instance = null;
        private static readonly object _padlock = new object();

        // Locks to synchronize file writing across multiple threads
        private static readonly object _logLock = new object();
        private static readonly object _stateLock = new object();

        private readonly string _logDirectory;
        private readonly string _stateFilePath;

        // Gets or sets the preferred log format. 
        // Defaults to "JSON" to ensure backward compatibility and prevent breaking changes.
        public string LogFormat { get; set; } = "JSON";

        // NEW : Property to choose the log target (Local, Serveur, Les Deux)
        public string LogTarget { get; set; } = "Les Deux";

        // NEW : Remote server configuration (LogServer)
        private readonly string _serverIp = "127.0.0.1"; // Local address
        private readonly int _serverPort = 11000;        // Same port as the server

        // Private constructor to prevent direct instantiation
        private LoggerService()
        {
            // --- START OF APPDATA MODIFICATIONS ---
            // Retrieve the user's AppData/Roaming directory to respect Windows permissions
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "ProSoft", "EasySave", "Logs");
            // --- END OF MODIFICATIONS ---

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

        // Appends a new log entry to the daily log file based on the configured LogFormat.
        // Added lock to prevent concurrent write operations from multiple threads
        public void WriteLog(LogModel logEntry)
        {
            lock (_logLock)
            {
                // 1. LOCAL WRITING (If selected in settings)
                if (LogTarget == "Local" || LogTarget == "Les Deux")
                {
                    List<LogModel> dailyLogs = new List<LogModel>();

                    // XML FORMAT HANDLING
                    if (LogFormat.ToUpper() == "XML")
                    {
                        string dailyLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.xml");

                        if (File.Exists(dailyLogFile))
                        {
                            try
                            {
                                // Read and deserialize the existing XML file content
                                XmlSerializer deserializer = new XmlSerializer(typeof(List<LogModel>));
                                using (TextReader reader = new StreamReader(dailyLogFile))
                                {
                                    dailyLogs = (List<LogModel>)deserializer.Deserialize(reader);
                                }
                            }
                            catch
                            {
                                // Initialize a new list if the file is corrupted or unreadable
                                dailyLogs = new List<LogModel>();
                            }
                        }

                        dailyLogs.Add(logEntry);

                        // Serialize and overwrite the updated list in XML format
                        XmlSerializer serializer = new XmlSerializer(typeof(List<LogModel>));
                        using (TextWriter writer = new StreamWriter(dailyLogFile))
                        {
                            serializer.Serialize(writer, dailyLogs);
                        }
                    }
                    // JSON FORMAT HANDLING (DEFAULT)
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
                }

                // 2. SERVER SENDING (If selected in settings)
                if (LogTarget == "Serveur" || LogTarget == "Les Deux")
                {
                    // NEW : Send unique logs to the distant server in real time
                    try
                    {
                        // Create a packet with the user's machine name + the log
                        var packet = new
                        {
                            User = Environment.MachineName,
                            Log = logEntry
                        };
                        string logJson = JsonSerializer.Serialize(packet, new JsonSerializerOptions { WriteIndented = true });
                        SendLogToServer(logJson);
                    }
                    catch { /* Silently ignore if sending fails to avoid crashing the application */ }
                }

            } // End of _logLock
        }

        // NEW : Method that handles network sending to the Console application (Docker)
        private void SendLogToServer(string logData)
        {
            try
            {
                using (TcpClient client = new TcpClient(_serverIp, _serverPort))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes(logData);
                    stream.Write(data, 0, data.Length);
                }
            }
            catch
            {
                // If the Docker server is down, suppress the error to prevent EasySave from crashing
            }
        }

        // Updates the real-time state file with the current progress of all tracked jobs.
        // The state file is strictly maintained in JSON format for optimized real-time reading.
        // Added lock to prevent concurrent write operations from multiple threads
        public void WriteState(List<StateModel> allStates)
        {
            lock (_stateLock)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(allStates, options);

                File.WriteAllText(_stateFilePath, jsonString);
            } // End of _stateLock
        }
    }
}