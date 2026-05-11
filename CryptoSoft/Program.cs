using System;
using System.IO;
using System.Text;
using System.Threading; // Required for Mutex
using System.Diagnostics; // Required for Stopwatch

namespace CryptoSoft
{
    class Program
    {
        // Define a unique Mutex to ensure CryptoSoft is Mono-Instance
        private static Mutex _mutex = new Mutex(true, "CryptoSoft_Unique_App_Mutex");

        static int Main(string[] args)
        {
            // Try to acquire the Mutex. TimeSpan.Zero means we don't wait if it's already taken.
            if (!_mutex.WaitOne(TimeSpan.Zero, true))
            {
                // If we can't get the Mutex, another instance is already running
                Console.WriteLine("Error: CryptoSoft is already running. Only one instance is allowed.");
                return -1; // Return -1 to signal EasySave that encryption failed
            }

            try
            {
                // Verify that both source and target file paths are provided as arguments
                if (args.Length < 2)
                {
                    return -1;
                }

                string sourceFile = args[0];
                string targetFile = args[1];

                // Start stopwatch to measure encryption time
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Retrieve the encryption key from a configuration file
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(appDirectory, "config.txt");
                string key = "EasySaveKey"; // Default fallback key

                // Read key from config file if it exists, otherwise create it
                if (File.Exists(configPath))
                {
                    key = File.ReadAllText(configPath);
                }
                else
                {
                    File.WriteAllText(configPath, key);
                }

                try
                {
                    byte[] fileBytes = File.ReadAllBytes(sourceFile);
                    byte[] keyBytes = Encoding.UTF8.GetBytes(key);

                    // Apply XOR encryption: perform a bitwise operation between file bytes and key bytes
                    for (int i = 0; i < fileBytes.Length; i++)
                    {
                        fileBytes[i] = (byte)(fileBytes[i] ^ keyBytes[i % keyBytes.Length]);
                    }

                    // Write the encrypted byte array to the target destination
                    File.WriteAllBytes(targetFile, fileBytes);

                    // Stop stopwatch
                    stopwatch.Stop();
                    int elapsedMs = (int)stopwatch.ElapsedMilliseconds;

                    // Return execution time (must be > 0). If execution is too fast, return 1ms minimum.
                    return elapsedMs > 0 ? elapsedMs : 1;
                }
                catch (Exception)
                {
                    return -1; // Return -1 to indicate an error during the encryption process
                }
            }
            finally
            {
                // Always release the Mutex when the application finishes, even if it crashes
                _mutex.ReleaseMutex();
            }
        }
    }
}