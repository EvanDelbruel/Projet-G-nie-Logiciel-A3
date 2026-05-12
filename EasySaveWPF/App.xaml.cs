using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using EasySaveWPF.Models;
using EasySaveWPF.Services;

namespace EasySaveWPF
{
    public partial class App : Application
    {
        // Asynchronous event handler enabling non-blocking execution of command-line operations prior to application shutdown.
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                // Headless CLI mode: Await the resolution of dispatched background tasks before gracefully terminating the process.
                await ExecuteCommandLineAsync(e.Args);
                Current.Shutdown();
            }
            else
            {
                // Interactive GUI mode: Bootstrap and display the primary presentation layer.
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        // Orchestrates headless backup executions based on standard input argument parsing.
        private async Task ExecuteCommandLineAsync(string[] args)
        {
            if (!File.Exists("jobs.json")) return;

            string json = File.ReadAllText("jobs.json");
            var allJobs = JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();

            if (allJobs.Count == 0) return;

            string input = string.Join("", args);
            List<int> jobsToRun = new List<int>();

            try
            {
                // Evaluate and parse range expressions (e.g., "1-3")
                if (input.Contains("-"))
                {
                    var parts = input.Split('-');
                    int start = int.Parse(parts[0]);
                    int end = int.Parse(parts[1]);

                    for (int i = start; i <= end; i++) jobsToRun.Add(i);
                }
                // Evaluate and parse delimited list expressions (e.g., "1;3;4")
                else if (input.Contains(";"))
                {
                    var parts = input.Split(';');
                    foreach (var part in parts) jobsToRun.Add(int.Parse(part));
                }
                // Evaluate discrete numeric inputs
                else
                {
                    jobsToRun.Add(int.Parse(input));
                }

                BackupService service = new BackupService();
                var runningTasks = new List<Task>();

                // Map parsed execution indices to persisted job definitions and queue them for concurrent processing.
                foreach (int index in jobsToRun)
                {
                    // Adjust base-1 human input to base-0 array indexing
                    int realIndex = index - 1;
                    if (realIndex >= 0 && realIndex < allJobs.Count)
                    {
                        var jobToExecute = allJobs[realIndex];

                        // Dispatch the asynchronous backup routine to the underlying service layer.
                        runningTasks.Add(service.ExecuteBackupAsync(jobToExecute, allJobs));
                    }
                }

                // Await the resolution of all parallel IO-bound operations to guarantee data integrity before process exit.
                await Task.WhenAll(runningTasks);
            }
            catch (Exception ex)
            {
                // Surface initialization or parsing faults to standard output in headless mode.
                Console.WriteLine($"CLI Execution Fault: {ex.Message}");
            }
        }
    }
}