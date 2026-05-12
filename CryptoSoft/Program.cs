using System;
using System.IO;
using System.Text;
using System.Threading; // Required for Mutex

namespace CryptoSoft
{
    class Program
    {
        // Defines a unique Mutex to ensure CryptoSoft is a single-instance application
        private static Mutex _mutex = new Mutex(true, "CryptoSoft_Unique_App_Mutex");

        static int Main(string[] args)
        {
            // Attempts to acquire the Mutex. TimeSpan.Zero means it does not wait if it is already taken.
            if (!_mutex.WaitOne(TimeSpan.Zero, true))
            {
                // If the Mutex cannot be acquired, another instance is already running.
                Console.WriteLine("Error: CryptoSoft is already running. Only one instance is allowed.");
                return -1; // Returns -1 to signal to EasySave that encryption failed (or was blocked)
            }

            try
            {
                // Checks that both the source and target file paths are provided as arguments
                if (args.Length < 2)
                {
                    return -1;
                }

                string sourceFile = args[0];
                string targetFile = args[1];

                // Defines the secret encryption key
                string key = "EasySaveKey";

                try
                {
                    byte[] fileBytes = File.ReadAllBytes(sourceFile);
                    byte[] keyBytes = Encoding.UTF8.GetBytes(key);

                    // Applies XOR encryption: bitwise operation between the file bytes and the key bytes
                    for (int i = 0; i < fileBytes.Length; i++)
                    {
                        fileBytes[i] = (byte)(fileBytes[i] ^ keyBytes[i % keyBytes.Length]);
                    }

                    // Writes the encrypted byte array to the target destination
                    File.WriteAllBytes(targetFile, fileBytes);

                    // Simulates a slight processing delay to ensure the execution time is visible in the logs
                    System.Threading.Thread.Sleep(50);

                    return 0; // Returns 0 to indicate successful execution
                }
                catch (Exception)
                {
                    return -1; // Returns -1 to indicate an error during the encryption process
                }
            }
            finally
            {
                // Always releases the Mutex when the application terminates, even in the event of a crash
                _mutex.ReleaseMutex();
            }
        }
    }
}