using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using EasyLog;
using EasySaveWPF.Models;

namespace EasySaveWPF.Services
{
    public class BackupService
    {
        public string BusinessSoftwareName { get; set; } = "notepad";

        public void ExecuteBackup(BackupJob activeJob, List<BackupJob> allJobs)
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

            List<string> filesToCopy = new List<string>();
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
                    filesToCopy.Add(file);
                    totalSize += new FileInfo(file).Length;
                }
            }

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

                    // ENCRYPTION LOGIC
                    if (cryptoExtensions.Contains(fileExtension))
                    {
                        Stopwatch swCrypto = Stopwatch.StartNew();

                        try
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
            finally
            {
                // This block ALWAYS executes, whether the backup finishes normally or is interrupted by the business software
                activeState.State = "Inactive";
                activeState.NbFilesLeftToDo = 0;

                // Set progress to 100% if the business software did not cause an interruption
                if (!IsBusinessSoftwareRunning())
                {
                    activeState.Progression = 100;
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