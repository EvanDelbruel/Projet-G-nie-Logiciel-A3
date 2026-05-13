using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EasyLog.Models;

namespace EasyLog
{
    // DESIGN PATTERN: Singleton Pattern
    // This class implements the Singleton pattern to guarantee that only one instance 
    // of LoggerService exists throughout the application lifecycle. This is crucial for
    // ensuring thread-safe logging and preventing file access conflicts across parallel tasks.
    // Ensures thread-safe writing operations across multiple backup jobs.
    public class LoggerService
    {
        private static LoggerService? _instance;
        private static readonly object _lock = new object(); // Lock object to prevent concurrent access issues

        // Gets the single instance of the LoggerService
        public static LoggerService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null) _instance = new LoggerService();
                    return _instance;
                }
            }
        }

        // --- Configuration Properties ---
        public string LogFormat { get; set; } = "JSON";

        public string LogDestination { get; set; } = "Local"; // Can be: "Local", "Docker", or "Both"
        public string DockerIp { get; set; } = "127.0.0.1";
        public int DockerPort { get; set; } = 11000;

        // Private constructor to enforce Singleton pattern
        private LoggerService() { }

        // Writes a log entry to the configured destinations (Local file and/or Docker server)
        public void WriteLog(LogModel log)
        {
            lock (_lock) // Ensure only one thread writes a log at a time
            {
                // 1. Write to LOCAL storage (if configured)
                if (LogDestination == "Local" || LogDestination == "Both")
                {
                    // Create the "Logs" directory in AppData to keep it accessible
                    string logsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "Logs");
                    if (!Directory.Exists(logsDirectory))
                    {
                        Directory.CreateDirectory(logsDirectory);
                    }

                    // Determine the file path based on the selected format
                    string filePath = LogFormat == "XML"
                        ? Path.Combine(logsDirectory, $"logs_{DateTime.Now:dd-MM-yyyy}.xml")
                        : Path.Combine(logsDirectory, $"logs_{DateTime.Now:dd-MM-yyyy}.json");

                    if (LogFormat == "XML")
                    {
                        WriteXml(log, filePath);
                    }
                    else
                    {
                        WriteJson(log, filePath);
                    }
                }

                // 2. Send to DOCKER server (if configured)
                if (LogDestination == "Docker" || LogDestination == "Both")
                {
                    // Always format as JSON for network transmission to ensure compatibility
                    string jsonLog = JsonSerializer.Serialize(log);

                    // Fire-and-forget: Launch the async network task without awaiting it.
                    // This prevents network latency from slowing down the file copy process.
                    _ = SendLogToDockerAsync(jsonLog);
                }
            }
        }

        // Overwrites the state.json file with the current progress of all active backup jobs
        public void WriteState(List<StateModel> states)
        {
            lock (_lock)
            {
                // Create the "State" directory in AppData to keep it accessible
                string stateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "State");
                if (!Directory.Exists(stateDirectory))
                {
                    Directory.CreateDirectory(stateDirectory);
                }

                string filePath = Path.Combine(stateDirectory, "state.json");

                // Serialize the list with indentation for better readability
                string json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
        }


        // Sends the log message asynchronously to the Docker log server via TCP.
        // Fails silently if the server is offline.
        private async Task SendLogToDockerAsync(string message)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Attempt to connect to the Docker server
                    var connectTask = client.ConnectAsync(DockerIp, DockerPort);

                    // Implement a 2-second timeout to avoid hanging the process if the server is unreachable
                    if (await Task.WhenAny(connectTask, Task.Delay(2000)) == connectTask)
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            // REQUIREMENT COMPLIANCE: Append the user's identity and machine name to the log message
                            string identity = $"[User: {Environment.UserName} | PC: {Environment.MachineName}] ";
                            string finalMessage = identity + message + Environment.NewLine;

                            byte[] data = Encoding.UTF8.GetBytes(finalMessage);
                            await stream.WriteAsync(data, 0, data.Length);
                            await stream.FlushAsync();
                        }
                    }
                }
            }
            catch
            {
                // Silent catch: If Docker is offline, the backup must continue without crashing
            }
        }

        // Appends a new log entry to an existing JSON file, or creates a new one
        private void WriteJson(LogModel log, string filePath)
        {
            List<LogModel> logs = new List<LogModel>();
            if (File.Exists(filePath))
            {
                try
                {
                    string existingJson = File.ReadAllText(filePath);
                    logs = JsonSerializer.Deserialize<List<LogModel>>(existingJson) ?? new List<LogModel>();
                }
                catch { }
            }
            logs.Add(log);
            File.WriteAllText(filePath, JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Appends a new log entry to an existing XML file, or creates a new one
        private void WriteXml(LogModel log, string filePath)
        {
            List<LogModel> logs = new List<LogModel>();
            XmlSerializer serializer = new XmlSerializer(typeof(List<LogModel>));

            if (File.Exists(filePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        logs = (List<LogModel>)(serializer.Deserialize(reader) ?? new List<LogModel>());
                    }
                }
                catch { }
            }

            logs.Add(log);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, logs);
            }
        }
    }
}