using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using EasySave.Models;
using EasyLog.Services;
using EasyLog.Models;

namespace EasySave.Services
{
    public class BackupService
    {
        private LoggerService logger = new LoggerService();

        // We now pass the active job AND the list of all jobs to generate the full state array
        public void ExecuteBackup(BackupJob activeJob, List<BackupJob> allJobs)
        {
            // Directory verification
            if (!Directory.Exists(activeJob.SourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {activeJob.SourceDirectory}");
            }
            if (!Directory.Exists(activeJob.TargetDirectory))
            {
                Directory.CreateDirectory(activeJob.TargetDirectory);
            }

            // Retrieve all files
            string[] files = Directory.GetFiles(activeJob.SourceDirectory, "*.*", SearchOption.AllDirectories);
            if (files.Length == 0) throw new Exception("Source directory is empty. No backup required.");

            // Prepare variables for state.json
            long totalSize = 0;
            foreach (string f in files) totalSize += new FileInfo(f).Length;

            int totalFiles = files.Length;
            int filesProcessed = 0;
            bool isDifferential = activeJob.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase);

            Console.WriteLine($"Copy progress: {totalFiles} file(s) detected...");

            // 1. INITIALIZE THE STATE ARRAY FOR ALL JOBS
            List<StateModel> allStates = new List<StateModel>();
            foreach (var job in allJobs)
            {
                allStates.Add(new StateModel
                {
                    Name = job.Name,
                    State = (job.Name == activeJob.Name) ? "Active" : "Inactive",
                    LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    TotalFilesToCopy = (job.Name == activeJob.Name) ? totalFiles : 0,
                    TotalFilesSize = (job.Name == activeJob.Name) ? totalSize : 0,
                    NbFilesLeftToDo = (job.Name == activeJob.Name) ? totalFiles : 0,
                    Progression = 0,
                    CurrentSourceFile = "",
                    CurrentTargetFile = ""
                });
            }

            // Get a reference to the active job's state to update it easily in the loop
            var activeState = allStates.First(s => s.Name == activeJob.Name);

            // 2. COPY LOOP
            foreach (string file in files)
            {
                string relativePath = Path.GetRelativePath(activeJob.SourceDirectory, file);
                string destFile = Path.Combine(activeJob.TargetDirectory, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                bool shouldCopy = true;

                // Check modification date if Differential mode
                if (isDifferential && File.Exists(destFile))
                {
                    if (File.GetLastWriteTime(file) <= File.GetLastWriteTime(destFile))
                    {
                        shouldCopy = false;
                    }
                }

                // If the file needs to be copied
                if (shouldCopy)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    File.Copy(file, destFile, true);
                    sw.Stop();

                    // Write to the daily log
                    logger.WriteLog(new LogModel
                    {
                        Name = activeJob.Name,
                        SourceFilePath = file,
                        TargetFilePath = destFile,
                        FileSize = new FileInfo(destFile).Length,
                        FileTransferTime = sw.Elapsed.TotalMilliseconds,
                        Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                    });
                }

                filesProcessed++;

                // 3. REAL-TIME STATE UPDATE
                activeState.CurrentSourceFile = file;
                activeState.CurrentTargetFile = destFile;
                activeState.NbFilesLeftToDo = totalFiles - filesProcessed;
                activeState.Progression = (int)((filesProcessed / (double)totalFiles) * 100);
                activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                // Send the entire list to the LoggerService
                logger.WriteState(allStates);
            }

            // 4. END OF BACKUP
            activeState.State = "Inactive";
            activeState.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            logger.WriteState(allStates);
        }
    }
}