using System;
using System.IO;
using System.Text;
using System.Threading; // Required for Mutex

namespace CryptoSoft
{
    class Program
    {
        // Define a unique Mutex to ensure CryptoSoft is Mono-Instance (V3.0 Requirement)
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

                // --- TA SUPERBE IDÉE : CONFIG.TXT ---
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
                // ------------------------------------

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

                    // Simulate a slight processing delay to ensure execution time > 0 in EasySave logs
                    Thread.Sleep(50);

                    return 0; // Return 0 to indicate SUCCESS to EasySave
                }
                catch (Exception)
                {
                    return -1; // Return -1 to indicate an ERROR during the encryption process
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