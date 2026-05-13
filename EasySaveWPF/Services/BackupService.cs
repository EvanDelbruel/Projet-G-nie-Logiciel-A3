using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EasyLog;
using EasySaveWPF.Models;

namespace EasySaveWPF.Services
{
    public class BackupService
    {
        public string BusinessSoftwareName { get; set; } = "notepad";

        // Maintains thread execution controllers for all active backup jobs to handle concurrency.
        private static readonly Dictionary<string, JobController> ActiveControllers = new Dictionary<string, JobController>();

        public void PauseJob(string jobName)
        {
            if (ActiveControllers.ContainsKey(jobName)) ActiveControllers[jobName].PauseEvent.Reset();
        }

        public void ResumeJob(string jobName)
        {
            if (ActiveControllers.ContainsKey(jobName)) ActiveControllers[jobName].PauseEvent.Set();
        }

        public void StopJob(string jobName)
        {
            if (ActiveControllers.ContainsKey(jobName)) 
            {
                ActiveControllers[jobName].CancelTokenSource.Cancel();
                ActiveControllers[jobName].PauseEvent.Set(); // Wake up thread if paused so it can stop
            }
        }

        public async Task ExecuteBackupAsync(BackupJob activeJob, List<BackupJob> allJobs)
        {
            var controller = new JobController();
            lock (ActiveControllers)
            {
                ActiveControllers[activeJob.Name] = controller;
            }

            var cancelToken = controller.CancelTokenSource.Token;
            var pauseEvent = controller.PauseEvent;

            // Application configuration parameters
            List<string> cryptoExtensions = new List<string>();
            List<string> priorityExtensions = new List<string> { ".txt" };
            long maxFileSizeLimitBytes = 50 * 1024;
            string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "settings.json");

            // Load application settings from the configuration file
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

                        if (settings.TryGetValue("CryptoExtensions", out string? cryptExt))
                            cryptoExtensions = cryptExt.Split(',').Select(e => e.Trim().ToLower()).ToList();

                        if (settings.TryGetValue("PriorityExtensions", out string? prioExt))
                            priorityExtensions = prioExt.Split(',').Select(e => e.Trim().ToLower()).ToList();

                        if (settings.TryGetValue("MaxFileSizeKb", out string? maxKbStr) && long.TryParse(maxKbStr, out long maxKb))
                            maxFileSizeLimitBytes = maxKb * 1024;
                    }
                }
                catch { } // Silently fallback to default values in case of parsing errors
            }

            if (!Directory.Exists(activeJob.SourceDirectory)) throw new DirectoryNotFoundException("Source directory not found.");
            if (!Directory.Exists(activeJob.TargetDirectory)) Directory.CreateDirectory(activeJob.TargetDirectory);

            string[] allFiles = Directory.GetFiles(activeJob.SourceDirectory, "*.*", SearchOption.AllDirectories);
            bool isDifferential = activeJob.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase);

            List<string> filesToCopy = new List<string>();
            long totalSize = 0;

            // Deadlock prevention: Track priority files specific to this execution context
            int localPriorityFilesCount = 0;

            foreach (string file in allFiles)
            {
                string destFile = Path.Combine(activeJob.TargetDirectory, Path.GetRelativePath(activeJob.SourceDirectory, file));
                bool shouldCopy = true;

                // Differential backup logic: skip if the target file is identical or newer
                if (isDifferential && File.Exists(destFile) && File.GetLastWriteTime(file) <= File.GetLastWriteTime(destFile))
                {
                    shouldCopy = false;
                }

                if (shouldCopy)
                {
                    filesToCopy.Add(file);
                    totalSize += new FileInfo(file).Length;

                    // Register priority constraints globally and track them locally
                    if (priorityExtensions.Contains(Path.GetExtension(file).ToLower()))
                    {
                        SyncManager.AddPriorityFile();
                        localPriorityFilesCount++;
                    }
                }
            }

            // Execution optimization: Sort files to process priority extensions first
            // This prevents resource starvation for lower-priority tasks.
            filesToCopy = filesToCopy.OrderByDescending(f => priorityExtensions.Contains(Path.GetExtension(f).ToLower())).ToList();

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
            bool wasStoppedByUser = false;
            long totalBytesProcessed = 0;

            try
            {
                await Task.Run(async () =>
                {
                    Application.Current.Dispatcher.Invoke(() => { activeJob.State = "Active"; });

                    foreach (string file in filesToCopy)
                    {
                        // Enforce cancellation requests
                        cancelToken.ThrowIfCancellationRequested();

                        // Enforce thread synchronization for user-initiated pauses
                        if (!pauseEvent.WaitOne(0))
                        {
                            Application.Current.Dispatcher.Invoke(() => { activeJob.State = "Paused"; });
                            activeState.State = "Paused";
                            LoggerService.Instance.WriteState(allStates);

                            pauseEvent.WaitOne(); // Suspend thread execution until signaled

                            Application.Current.Dispatcher.Invoke(() => { activeJob.State = "Active"; });
                            activeState.State = "Active";
                            LoggerService.Instance.WriteState(allStates);
                        }

                        // Enforce business software constraints (Automatic Pause)
                        bool wasPausedBySoftware = false;

                        while (IsBusinessSoftwareRunning())
                        {
                            if (!wasPausedBySoftware)
                            {
                                Application.Current.Dispatcher.Invoke(() => { activeJob.State = "Paused"; });
                                activeState.State = "Paused (Software)";
                                LoggerService.Instance.WriteState(allStates);
                                wasPausedBySoftware = true;

                                // Marshal UI notification to the main Dispatcher thread
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    System.Windows.MessageBox.Show(
                                        $"La sauvegarde '{activeJob.Name}' est en pause car le logiciel métier bloquant ({BusinessSoftwareName}) est ouvert.\n\nElle reprendra automatiquement dès que vous le fermerez.",
                                        "Logiciel métier détecté 🛑",
                                        System.Windows.MessageBoxButton.OK,
                                        System.Windows.MessageBoxImage.Warning);
                                });
                            }
                            await Task.Delay(1000, cancelToken); // Poll process state asynchronously
                        }

                        if (wasPausedBySoftware)
                        {
                            Application.Current.Dispatcher.Invoke(() => { activeJob.State = "Active"; });
                            activeState.State = "Active";
                            LoggerService.Instance.WriteState(allStates);

                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                System.Windows.MessageBox.Show(
                                    $"Le logiciel métier a été fermé. La sauvegarde '{activeJob.Name}' reprend !",
                                    "Reprise automatique ▶️",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Information);
                            });
                        }

                        string relativePath = Path.GetRelativePath(activeJob.SourceDirectory, file);
                        string destFile = Path.Combine(activeJob.TargetDirectory, relativePath);
                        string? directoryName = Path.GetDirectoryName(destFile);
                        if (!string.IsNullOrEmpty(directoryName)) Directory.CreateDirectory(directoryName);

                        string fileExtension = Path.GetExtension(file).ToLower();
                        long fileSize = new FileInfo(file).Length;

                        bool isPriorityFile = priorityExtensions.Contains(fileExtension);
                        bool isLargeFile = fileSize > maxFileSizeLimitBytes;
                        bool hasLargeFileLock = false;

                        // Priority synchronization: Block non-priority files if priority files are pending globally
                        if (!isPriorityFile)
                        {
                            while (SyncManager.ArePriorityFilesWaiting())
                            {
                                await Task.Delay(100, cancelToken);
                            }
                        }

                        // Bandwidth optimization: Throttle simultaneous large file transfers
                        if (isLargeFile)
                        {
                            await SyncManager.LargeFileSemaphore.WaitAsync(cancelToken);
                            hasLargeFileLock = true;
                        }

                        try
                        {
                            double transferTimeMs = 0;
                            double encryptTimeMs = 0;

                            // Encryption requirement: Enforce single-instance execution constraint
                            if (cryptoExtensions.Contains(fileExtension))
                            {
                                await SyncManager.CryptoSemaphore.WaitAsync(cancelToken);
                                Stopwatch swCrypto = Stopwatch.StartNew();
                                try
                                {
                                    string cryptoPath = Path.GetFullPath(@"CryptoSoftTool\CryptoSoft.exe");
                                    if (!File.Exists(cryptoPath))
                                    {
                                        Console.WriteLine($"[ERROR] Missing executable at path: {cryptoPath}");
                                        throw new FileNotFoundException("CryptoSoft.exe is missing.");
                                    }

                                    Process cryptoProcess = new Process();
                                    cryptoProcess.StartInfo.FileName = cryptoPath;
                                    cryptoProcess.StartInfo.Arguments = $"\"{file}\" \"{destFile}\"";
                                    cryptoProcess.StartInfo.UseShellExecute = false;
                                    cryptoProcess.StartInfo.CreateNoWindow = true;
                                    cryptoProcess.Start();
                                    using (cancelToken.Register(() => { try { if (!cryptoProcess.HasExited) cryptoProcess.Kill(); } catch { } }))
                                    {
                                        cryptoProcess.WaitForExit();
                                    }

                                    if (cryptoProcess.ExitCode != 0) throw new Exception("Encryption process returned a non-zero exit code.");

                                    swCrypto.Stop();
                                    encryptTimeMs = swCrypto.Elapsed.TotalMilliseconds;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Encryption Exception: {ex.Message}");
                                    swCrypto.Stop();
                                    encryptTimeMs = -1;
                                    
                                    if (cancelToken.IsCancellationRequested)
                                    {
                                        if (File.Exists(destFile)) File.Delete(destFile);
                                        throw new OperationCanceledException();
                                    }

                                    File.Copy(file, destFile, true); // Fallback: Proceed with standard file copy

                                    totalBytesProcessed += fileSize;
                                    int fallbackProgression = totalSize > 0 ? (int)((totalBytesProcessed / (double)totalSize) * 100) : 100;
                                    activeState.Progression = fallbackProgression;
                                    Application.Current.Dispatcher.Invoke(() => { activeJob.Progression = fallbackProgression; });
                                }
                                finally
                                {
                                    SyncManager.CryptoSemaphore.Release();
                                }

                                // Success for CryptoSoft
                                if (encryptTimeMs != -1)
                                {
                                    totalBytesProcessed += fileSize;
                                    int cryptoProgression = totalSize > 0 ? (int)((totalBytesProcessed / (double)totalSize) * 100) : 100;
                                    activeState.Progression = cryptoProgression;
                                    Application.Current.Dispatcher.Invoke(() => { activeJob.Progression = cryptoProgression; });
                                }
                            }
                            else
                            {
                                // Standard file transfer operation (Smooth Progress)
                                Stopwatch swTransfer = Stopwatch.StartNew();
                                try
                                {
                                    int bufferSize = 1024 * 1024; // 1 MB buffer
                                    using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                                    using (FileStream destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                                    {
                                        byte[] buffer = new byte[bufferSize];
                                        int bytesRead;
                                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            cancelToken.ThrowIfCancellationRequested();

                                            // Enforce thread synchronization for user-initiated pauses during chunk copy
                                            if (!pauseEvent.WaitOne(0))
                                            {
                                                Application.Current.Dispatcher.Invoke(() => { activeJob.State = "Paused"; });
                                                activeState.State = "Paused";
                                                LoggerService.Instance.WriteState(allStates);
                                                pauseEvent.WaitOne();
                                                Application.Current.Dispatcher.Invoke(() => { activeJob.State = "Active"; });
                                                activeState.State = "Active";
                                                LoggerService.Instance.WriteState(allStates);
                                            }

                                            destStream.Write(buffer, 0, bytesRead);

                                            totalBytesProcessed += bytesRead;
                                            int currentProgression = totalSize > 0 ? (int)((totalBytesProcessed / (double)totalSize) * 100) : 100;

                                            // Update UI and JSON only when percentage changes to avoid lag
                                            if (currentProgression != activeState.Progression)
                                            {
                                                activeState.Progression = currentProgression;
                                                Application.Current.Dispatcher.Invoke(() => { activeJob.Progression = currentProgression; });
                                                LoggerService.Instance.WriteState(allStates);
                                            }
                                        }
                                    }
                                    swTransfer.Stop();
                                    transferTimeMs = swTransfer.Elapsed.TotalMilliseconds;
                                }
                                catch (Exception ex) 
                                { 
                                    swTransfer.Stop(); 
                                    transferTimeMs = -1; 
                                    if (ex is OperationCanceledException)
                                    {
                                        if (File.Exists(destFile)) File.Delete(destFile);
                                        throw;
                                    }
                                }
                            }

                            // Commit telemetry data
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

                            // Update application state
                            filesProcessed++;
                            activeState.CurrentSourceFile = file;
                            activeState.CurrentTargetFile = destFile;
                            activeState.NbFilesLeftToDo = totalFiles - filesProcessed;
                            activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                            LoggerService.Instance.WriteState(allStates);
                        }
                        finally
                        {
                            // Ensure synchronization primitives are released regardless of task success/failure
                            if (hasLargeFileLock) SyncManager.LargeFileSemaphore.Release();

                            if (isPriorityFile)
                            {
                                SyncManager.RemovePriorityFile();
                                localPriorityFilesCount--;
                            }
                        }
                    }
                }, cancelToken);
            }
            catch (OperationCanceledException)
            {
                wasStoppedByUser = true;
            }
            finally
            {
                // Graceful degradation: Release remaining global locks if task was aborted mid-execution
                while (localPriorityFilesCount > 0)
                {
                    SyncManager.RemovePriorityFile();
                    localPriorityFilesCount--;
                }

                lock (ActiveControllers)
                {
                    if (ActiveControllers.ContainsKey(activeJob.Name)) ActiveControllers.Remove(activeJob.Name);
                }

                activeState.State = wasStoppedByUser ? "Stopped" : "Inactive";

                if (wasStoppedByUser)
                {
                    activeState.CurrentSourceFile = "STOPPED";
                    activeState.CurrentTargetFile = "STOPPED";

                    Application.Current.Dispatcher.Invoke(() => {
                        activeJob.Progression = 0;
                        activeJob.State = "Inactive";
                    });
                }
                else
                {
                    activeState.NbFilesLeftToDo = 0;
                    activeState.Progression = 100;
                    activeState.CurrentSourceFile = "";
                    activeState.CurrentTargetFile = "";

                    Application.Current.Dispatcher.Invoke(() => {
                        activeJob.Progression = 0;
                        activeJob.State = "Inactive";
                    });
                }

                activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                LoggerService.Instance.WriteState(allStates);
            }
        }

        // Validates the execution state of the specified business software
        private bool IsBusinessSoftwareRunning()
        {
            if (string.IsNullOrWhiteSpace(BusinessSoftwareName)) return false;
            Process[] processes = Process.GetProcessesByName(BusinessSoftwareName);
            return processes.Length > 0;
        }
    }
}