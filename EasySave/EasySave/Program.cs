using System;
using EasyLog;

namespace EasySave
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Bienvenue dans EasySave V1.0 !");
            Console.WriteLine("Test de la création d'un Log JSON en cours...");

            //  DLL qui créer un log de test
            LogManager.SaveLog("Backup_Test_01", @"C:\DossierSource", @"C:\DossierCible", 4096, 25.5);

            Console.WriteLine("Validé ! dans la section Documents.");

            Console.ReadLine();
        }
    }
}