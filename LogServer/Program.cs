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
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    EasySave Centralized Log Server     ");
            Console.WriteLine("========================================");

            int port = 11000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);

            try
            {
                listener.Start();
                Console.WriteLine($"[Serveur] En écoute sur le port {port}...");
                Console.WriteLine("[Serveur] Prêt à recevoir les logs d'EasySave.\n");

                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erreur] {ex.Message}");
            }
            finally
            {
                listener.Stop();
            }
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string rawData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string serverLogFile = "centralized_logs.json";
                        lock (typeof(Program))
                        {
                            File.AppendAllText(serverLogFile, rawData + Environment.NewLine);
                        }

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LOG REÇU (sauvegardé dans {serverLogFile})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erreur Client] {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}