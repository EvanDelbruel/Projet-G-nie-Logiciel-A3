using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization; // Required for XML serialization
using System.Net.Sockets;       // NOUVEAU : Required for TCP network (Phase 4)
using System.Text;              // NOUVEAU : Required for encoding (Phase 4)

namespace EasyLog
{
    // Singleton service responsible for handling application logs and real-time state tracking.
    // Supports both JSON and XML file formats for daily logs.
    public sealed class LoggerService
    {
        // Static variables for the Singleton pattern implementation and thread synchronization
        private static LoggerService _instance = null;
        private static readonly object _padlock = new object();

        //  Locks to synchronize file writing across multiple threads
        private static readonly object _logLock = new object();
        private static readonly object _stateLock = new object();

        private readonly string _logDirectory = "Logs";
        private readonly string _stateFilePath;

        // Gets or sets the preferred log format. 
        // Defaults to "JSON" to ensure backward compatibility and prevent breaking changes.
        public string LogFormat { get; set; } = "JSON";

        // NOUVEAU : Propriété pour choisir la cible des logs (Local, Serveur, Les Deux)
        public string LogTarget { get; set; } = "Les Deux";

        // NOUVEAU : Configuration du serveur distant (LogServer)
        private readonly string _serverIp = "127.0.0.1"; // Adresse locale
        private readonly int _serverPort = 11000;        // Le même port que le serveur

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

        // Appends a new log entry to the daily log file based on the configured LogFormat.
        //  Added lock to prevent concurrent write operations from multiple threads
        public void WriteLog(LogModel logEntry)
        {
            lock (_logLock)
            {
                // 1. ÉCRITURE LOCALE (Si sélectionné dans les paramètres)
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

                // 2. ENVOI SERVEUR (Si sélectionné dans les paramètres)
                if (LogTarget == "Serveur" || LogTarget == "Les Deux")
                {
                    // NOUVEAU : Envoi du log unique au serveur distant en temps réel
                    try
                    {
                        // On crée un paquet avec le nom de la machine de l'utilisateur + le log
                        var packet = new
                        {
                            User = Environment.MachineName,
                            Log = logEntry
                        };
                        string logJson = JsonSerializer.Serialize(packet, new JsonSerializerOptions { WriteIndented = true });
                        SendLogToServer(logJson);
                    }
                    catch { /* On ignore silencieusement si l'envoi échoue pour ne pas bloquer l'application */ }
                }

            } // End of _logLock
        }

        // NOUVEAU : Méthode qui s'occupe de l'envoi réseau vers l'application Console
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
                // Si le serveur Docker est éteint, on étouffe l'erreur pour ne pas faire planter EasySave
            }
        }

        // Updates the real-time state file with the current progress of all tracked jobs.
        // The state file is strictly maintained in JSON format for optimized real-time reading.
        //  Added lock to prevent concurrent write operations from multiple threads
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