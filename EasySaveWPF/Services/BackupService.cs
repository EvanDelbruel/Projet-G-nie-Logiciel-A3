using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading; // Added for SemaphoreSlim and ManualResetEvent
using EasyLog;
using EasySaveWPF.Models;

namespace EasySaveWPF.Services
{
    public class BackupService
    {
        public string BusinessSoftwareName { get; set; } = "notepad";

        // Default limit for large files (50 MB in bytes)
        public long MaxFileSize { get; set; } = 52428800;

        // List to store the priority extensions from settings
        public List<string> PriorityExtensions { get; set; } = new List<string>();

        // Static lock shared across all instances of BackupService
        // Ensures only one thread can execute CryptoSoft at a time
        private static readonly object _cryptoLock = new object();

        // Semaphore to limit the simultaneous transfer of large files to EXACTLY ONE
        private static readonly SemaphoreSlim _largeFileSemaphore = new SemaphoreSlim(1, 1);

        // The "Traffic Light" for priority files. Starts as GREEN (Signaled)
        private static readonly ManualResetEvent _priorityTrafficLight = new ManualResetEvent(true);

        // Counter to track how many priority files are currently being processed across ALL jobs
        private static int _priorityFilesInProgress = 0;
        private static readonly object _priorityCounterLock = new object();

        // NOUVEAU : Ajout des arguments pauseEvent et cancellationToken
        public void ExecuteBackup(BackupJob activeJob, List<BackupJob> allJobs, ManualResetEvent pauseEvent, CancellationToken cancellationToken)
        {
            // Load settings for the business software and encryption extensions
            List<string> cryptoExtensions = new List<string>();
            string settingsFilePath = "settings.json";

            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string jsonSettings = File.ReadAllText(settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonSettings);

                    if (settings != null)
                    {
                        if (settings.TryGetValue("BusinessSoftware", out string? software))
                            BusinessSoftwareName = software;

                        if (settings.TryGetValue("CryptoExtensions", out string? extensions))
                        {
                            // Parse the extension list and convert to lowercase
                            cryptoExtensions = extensions.Split(',')
                                                         .Select(e => e.Trim().ToLower())
                                                         .ToList();
                        }

                        // Load the max file size limit from settings if it exists
                        if (settings.TryGetValue("MaxFileSize", out string? maxSizeStr) && long.TryParse(maxSizeStr, out long maxSize))
                        {
                            MaxFileSize = maxSize;
                        }

                        // Load priority extensions from settings
                        if (settings.TryGetValue("PriorityExtensions", out string? priorityExts))
                        {
                            PriorityExtensions = priorityExts.Split(',').Select(e => e.Trim().ToLower()).ToList();
                        }
                    }
                }
                catch { } // Retain default values if an error occurs during reading or deserialization
            }

            if (IsBusinessSoftwareRunning())
            {
                throw new InvalidOperationException($"The target business software ('{BusinessSoftwareName}') is currently running. Please close it.");
            }

            if (!Directory.Exists(activeJob.SourceDirectory)) throw new DirectoryNotFoundException("Source directory not found.");
            if (!Directory.Exists(activeJob.TargetDirectory)) Directory.CreateDirectory(activeJob.TargetDirectory);

            string[] allFiles = Directory.GetFiles(activeJob.SourceDirectory, "*.*", SearchOption.AllDirectories);
            bool isDifferential = activeJob.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase);

            // We separate files into two lists: Priority and Normal
            List<string> priorityFilesToCopy = new List<string>();
            List<string> normalFilesToCopy = new List<string>();

            long totalSize = 0;

            foreach (string file in allFiles)
            {
                string destFile = Path.Combine(activeJob.TargetDirectory, Path.GetRelativePath(activeJob.SourceDirectory, file));
                bool shouldCopy = true;

                if (isDifferential && File.Exists(destFile) && File.GetLastWriteTime(file) <= File.GetLastWriteTime(destFile))
                {
                    shouldCopy = false;
                }

                if (shouldCopy)
                {
                    totalSize += new FileInfo(file).Length;
                    string extension = Path.GetExtension(file).ToLower();

                    // Sort file into the correct list
                    if (PriorityExtensions.Contains(extension))
                    {
                        priorityFilesToCopy.Add(file);
                    }
                    else
                    {
                        normalFilesToCopy.Add(file);
                    }
                }
            }

            // Combine the lists. Priority files MUST be processed first in this job.
            List<string> filesToCopy = new List<string>();
            filesToCopy.AddRange(priorityFilesToCopy);
            filesToCopy.AddRange(normalFilesToCopy);

            int totalFiles = filesToCopy.Count;
            int filesProcessed = 0;

            List<EasyLog.Models.StateModel> allStates = new List<EasyLog.Models.StateModel>();
            foreach (var job in allJobs)
            {
                allStates.Add(new EasyLog.Models.StateModel
                {
                    Name = job.Name,
                    State = (job.Name == activeJob.Name && totalFiles > 0) ? "Active" : "Inactive",
                    LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    TotalFilesToCopy = (job.Name == activeJob.Name) ? totalFiles : 0,
                    TotalFilesSize = (job.Name == activeJob.Name) ? totalSize : 0,
                    NbFilesLeftToDo = (job.Name == activeJob.Name) ? totalFiles : 0,
                    Progression = (totalFiles == 0 && job.Name == activeJob.Name) ? 100 : 0,
                    CurrentSourceFile = "",
                    CurrentTargetFile = ""
                });
            }

            var activeState = allStates.First(s => s.Name == activeJob.Name);

            // Use a try-finally block to ensure the state file is reliably updated upon completion or failure
            try
            {
                foreach (string file in filesToCopy)
                {
                    // Verification stop and pause before  paste file
                    cancellationToken.ThrowIfCancellationRequested();
                    pauseEvent.WaitOne();

                    if (IsBusinessSoftwareRunning())
                    {
                        LoggerService.Instance.WriteLog(new EasyLog.Models.LogModel
                        {
                            Name = activeJob.Name,
                            SourceFilePath = "INTERRUPTED",
                            TargetFilePath = $"Software: {BusinessSoftwareName}",
                            FileSize = 0,
                            FileTransferTime = -1,
                            FileEncryptTime = 0,
                            Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                        });
                        throw new InvalidOperationException("Backup interrupted by the business software.");
                    }

                    string relativePath = Path.GetRelativePath(activeJob.SourceDirectory, file);
                    string destFile = Path.Combine(activeJob.TargetDirectory, relativePath);

                    string? directoryName = Path.GetDirectoryName(destFile);
                    if (!string.IsNullOrEmpty(directoryName)) Directory.CreateDirectory(directoryName);

                    double transferTimeMs = 0;
                    double encryptTimeMs = 0;
                    long fileSize = new FileInfo(file).Length;
                    string fileExtension = Path.GetExtension(file).ToLower();

                    // Check if the file is considered "Large" or "Priority"
                    bool isLargeFile = fileSize >= MaxFileSize;
                    bool isPriorityFile = PriorityExtensions.Contains(fileExtension);

                    // GLOBAL PRIORITY CHECK
                    if (isPriorityFile)
                    {
                        // I am a priority file. I turn the traffic light RED for normal files.
                        lock (_priorityCounterLock)
                        {
                            _priorityFilesInProgress++;
                            _priorityTrafficLight.Reset(); // RED LIGHT
                        }
                    }
                    else
                    {
                        // I am a normal file. I must wait if the traffic light is RED.
                        _priorityTrafficLight.WaitOne();
                    }

                    try
                    {
                        // If it's a large file, the thread must wait for the Semaphore ticket
                        if (isLargeFile)
                        {
                            _largeFileSemaphore.Wait();
                        }

                        // ENCRYPTION LOGIC
                        if (cryptoExtensions.Contains(fileExtension))
                        {
                            Stopwatch swCrypto = Stopwatch.StartNew();

                            try
                            {
                                // Use the lock to force threads to wait their turn for CryptoSoft
                                lock (_cryptoLock)
                                {
                                    Process cryptoProcess = new Process();
                                    cryptoProcess.StartInfo.FileName = @"CryptoSoftTool\CryptoSoft.exe";
                                    cryptoProcess.StartInfo.Arguments = $"\"{file}\" \"{destFile}\"";
                                    cryptoProcess.StartInfo.UseShellExecute = false;
                                    cryptoProcess.StartInfo.CreateNoWindow = true;

                                    cryptoProcess.Start();
                                    cryptoProcess.WaitForExit();

                                    if (cryptoProcess.ExitCode != 0)
                                    {
                                        throw new Exception("Internal CryptoSoft error.");
                                    }
                                } // End of lock

                                swCrypto.Stop();
                                encryptTimeMs = swCrypto.Elapsed.TotalMilliseconds;
                            }
                            catch (Exception)
                            {
                                swCrypto.Stop();
                                encryptTimeMs = -1; // Flag the encryption failure in the logs
                                File.Copy(file, destFile, true); // Fallback: perform a standard copy
                            }
                        }
                        else
                        {
                            // Standard file copy without encryption
                            Stopwatch swTransfer = Stopwatch.StartNew();
                            try
                            {
                                File.Copy(file, destFile, true);
                                swTransfer.Stop();
                                transferTimeMs = swTransfer.Elapsed.TotalMilliseconds;
                            }
                            catch (Exception)
                            {
                                swTransfer.Stop();
                                transferTimeMs = -1;
                            }
                        }
                    }
                    finally
                    {
                        // Always release the Semaphore ticket when the large file is done
                        if (isLargeFile)
                        {
                            _largeFileSemaphore.Release();
                        }

                        // Release priority lock if I was a priority file
                        if (isPriorityFile)
                        {
                            lock (_priorityCounterLock)
                            {
                                _priorityFilesInProgress--;
                                // If I was the LAST priority file across ALL jobs, turn the traffic light GREEN
                                if (_priorityFilesInProgress == 0)
                                {
                                    _priorityTrafficLight.Set(); // GREEN LIGHT
                                }
                            }
                        }
                    }

                    // WRITE LOG ENTRY INCLUDING ENCRYPTION TIME
                    LoggerService.Instance.WriteLog(new EasyLog.Models.LogModel
                    {
                        Name = activeJob.Name,
                        SourceFilePath = file,
                        TargetFilePath = destFile,
                        FileSize = fileSize,
                        FileTransferTime = transferTimeMs,
                        FileEncryptTime = encryptTimeMs,
                        Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                    });

                    filesProcessed++;

                    activeState.CurrentSourceFile = file;
                    activeState.CurrentTargetFile = destFile;
                    activeState.NbFilesLeftToDo = totalFiles - filesProcessed;
                    activeState.Progression = (int)((filesProcessed / (double)totalFiles) * 100);
                    activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    LoggerService.Instance.WriteState(allStates);
                }
            }
            catch (OperationCanceledException)
            {
                // NOUVEAU : Gère l'état si on a cliqué sur STOP
                activeState.State = "STOPPED";
                LoggerService.Instance.WriteState(allStates);
                throw;
            }
            finally
            {
                // This block ALWAYS executes, whether the backup finishes normally or is interrupted by the business software
                if (activeState.State != "STOPPED")
                {
                    activeState.State = "Inactive";
                    activeState.NbFilesLeftToDo = 0;

                    // Set progress to 100% if the business software did not cause an interruption
                    if (!IsBusinessSoftwareRunning())
                    {
                        activeState.Progression = 100;
                    }
                }

                activeState.CurrentSourceFile = "";
                activeState.CurrentTargetFile = "";
                activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                LoggerService.Instance.WriteState(allStates);
            }
        }

        private bool IsBusinessSoftwareRunning()
        {
            if (string.IsNullOrWhiteSpace(BusinessSoftwareName)) return false;
            Process[] processes = Process.GetProcessesByName(BusinessSoftwareName);
            return processes.Length > 0;
        }
    }
}