using System;
using System.IO;
using System.Text;

namespace CryptoSoft
{
    class Program
    {
        static int Main(string[] args)
        {
            // Verify that both source and target file paths are provided as arguments
            if (args.Length < 2)
            {
                return -1;
            }

            string sourceFile = args[0];
            string targetFile = args[1];

            // Define the secret encryption key
            string key = "EasySaveKey";

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

                // Simulate a slight processing delay to ensure execution time is visible in the logs
                System.Threading.Thread.Sleep(50);

                return 0; // Return 0 to indicate successful execution
            }
            catch (Exception)
            {
                return -1; // Return -1 to indicate an error during the encryption process
            }
        }
    }
}