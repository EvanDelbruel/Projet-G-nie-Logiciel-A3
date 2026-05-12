using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LogServer
{
    class Program
    {
        // Global synchronization primitive to safely manage concurrent file I/O access across multiple client threads.
        private static readonly object _fileLock = new object();

        static async Task Main(string[] args)
        {
            int port = 11000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"[Server] Ready and listening on port {port}...");

            // Establishes a continuous listener loop to accept incoming TCP client connections asynchronously.
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                // Dispatches the client connection to the thread pool for concurrent processing without blocking the main listener.
                _ = HandleClientAsync(client);
            }
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string? logMessage = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(logMessage))
                    {
                        // Derives the dynamic daily log file path based on the current system date.
                        string fileName = $"logs_{DateTime.Now:dd-MM-yyyy}.json";

                        // Enforces thread synchronization using a static lock to prevent file access violations 
                        // when multiple remote instances attempt to write telemetry data simultaneously.
                        lock (_fileLock)
                        {
                            File.AppendAllText(fileName, logMessage + Environment.NewLine);
                        }

                        Console.WriteLine($"[OK] Log received and persisted at {DateTime.Now:HH:mm:ss}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Execution fault during client handling: {ex.Message}");
            }
            finally
            {
                // Ensures network resources and socket connections are reliably released after the payload is processed.
                client.Close();
            }
        }
    }
}