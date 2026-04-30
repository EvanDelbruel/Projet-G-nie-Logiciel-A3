using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using EasySaveWPF.Models;
using EasySaveWPF.Services;

namespace EasySaveWPF
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Check if command-line arguments were provided at launch
            if (e.Args.Length > 0)
            {
                // CLI Mode: Execute the requested backup jobs and terminate the application silently
                ExecuteCommandLine(e.Args);
                Current.Shutdown();
            }
            else
            {
                // GUI Mode: Launch the standard main window interface
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        private void ExecuteCommandLine(string[] args)
        {
            // Abort execution if the jobs configuration file is missing
            if (!File.Exists("jobs.json")) return;

            // Load and deserialize all configured backup jobs from the local storage
            string json = File.ReadAllText("jobs.json");
            var allJobs = JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();

            if (allJobs.Count == 0) return;

            // Concatenate arguments to handle spacing issues and extract target inputs
            string input = string.Join("", args);
            List<int> jobsToRun = new List<int>();

            // Parse the command-line input to identify which specific jobs to execute
            try
            {
                if (input.Contains("-")) // Handles sequential range inputs (e.g., "1-3")
                {
                    var parts = input.Split('-');
                    int start = int.Parse(parts[0]);
                    int end = int.Parse(parts[1]);

                    for (int i = start; i <= end; i++)
                    {
                        jobsToRun.Add(i);
                    }
                }
                else if (input.Contains(";")) // Handles specific list inputs (e.g., "1;3")
                {
                    var parts = input.Split(';');
                    foreach (var part in parts)
                    {
                        jobsToRun.Add(int.Parse(part));
                    }
                }
                else // Handles single job execution (e.g., "2")
                {
                    jobsToRun.Add(int.Parse(input));
                }

                // Iterate through the targeted IDs and execute the corresponding backup processes
                BackupService service = new BackupService();
                foreach (int index in jobsToRun)
                {
                    // Convert user-friendly 1-based index to standard 0-based list index
                    int realIndex = index - 1;
                    if (realIndex >= 0 && realIndex < allJobs.Count)
                    {
                        var jobToExecute = allJobs[realIndex];
                        try
                        {
                            service.ExecuteBackup(jobToExecute, allJobs);
                        }
                        catch (Exception ex)
                        {
                            // Output error to the console if a business software conflict interrupts the process
                            Console.WriteLine($"Error on job {index}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore formatting errors in the command-line arguments to prevent application crashes
            }
        }
    }
}