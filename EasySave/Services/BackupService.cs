using EasyLog;
using EasyLog.Models;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
// using EasySave.Models;

namespace EasySave.Services
{
    public class BackupService
    {
        public void ExecuteBackup(BackupJob activeJob, List<BackupJob> allJobs)
        {
            // Step 1: Verify source and target directories
            if (!Directory.Exists(activeJob.SourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {activeJob.SourceDirectory}");
            }
            if (!Directory.Exists(activeJob.TargetDirectory))
            {
                Directory.CreateDirectory(activeJob.TargetDirectory);
            }

            string[] allFiles = Directory.GetFiles(activeJob.SourceDirectory, "*.*", SearchOption.AllDirectories);
            bool isDifferential = activeJob.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase);

            // Step 2: Pre-filter files to calculate accurate metrics for the state file
            List<string> filesToCopy = new List<string>();
            long totalSize = 0;

            foreach (string file in allFiles)
            {
                string relativePath = Path.GetRelativePath(activeJob.SourceDirectory, file);
                string destFile = Path.Combine(activeJob.TargetDirectory, relativePath);

                bool shouldCopy = true;

                // Differential logic: skip the file if it hasn't been modified since the last backup
                if (isDifferential && File.Exists(destFile))
                {
                    if (File.GetLastWriteTime(file) <= File.GetLastWriteTime(destFile))
                    {
                        shouldCopy = false;
                    }
                }

                if (shouldCopy)
                {
                    filesToCopy.Add(file);
                    totalSize += new FileInfo(file).Length;
                }
            }

            int totalFiles = filesToCopy.Count;
            int filesProcessed = 0;

            // Step 3: Initialize the state array with accurate eligible file counts
            List<StateModel> allStates = new List<StateModel>();
            foreach (var job in allJobs)
            {
                allStates.Add(new StateModel
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

            // Step 4: Execute the copy loop only on eligible files
            foreach (string file in filesToCopy)
            {
                string relativePath = Path.GetRelativePath(activeJob.SourceDirectory, file);
                string destFile = Path.Combine(activeJob.TargetDirectory, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                // Measure file transfer time accurately
                Stopwatch sw = Stopwatch.StartNew();
                File.Copy(file, destFile, true);
                sw.Stop();

                // Call the Singleton instance to log the action
                LoggerService.Instance.WriteLog(new LogModel
                {
                    Name = activeJob.Name,
                    SourceFilePath = file,
                    TargetFilePath = destFile,
                    FileSize = new FileInfo(destFile).Length,
                    FileTransferTime = sw.Elapsed.TotalMilliseconds,
                    Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                });

                filesProcessed++;

                // Update real-time state variables
                activeState.CurrentSourceFile = file;
                activeState.CurrentTargetFile = destFile;
                activeState.NbFilesLeftToDo = totalFiles - filesProcessed;
                activeState.Progression = (int)((filesProcessed / (double)totalFiles) * 100);
                activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                // Call the Singleton instance to update the state file
                LoggerService.Instance.WriteState(allStates);
            }

            // Step 5: Finalize the state file once the job is completed
            activeState.State = "Inactive";
            activeState.NbFilesLeftToDo = 0;
            activeState.Progression = 100;
            activeState.CurrentSourceFile = "";
            activeState.CurrentTargetFile = "";
            activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            LoggerService.Instance.WriteState(allStates);
        }
    }
}