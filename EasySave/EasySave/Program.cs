using System;
using System.Collections.Generic;
using EasySave.Views;

namespace EasySave
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainView view = new MainView();

            if (args.Length > 0)
            {
                // On récupère l'argument (ex: "1-3")
                string input = args[0];

                // NOUVEAU : On utilise la méthode de la vue pour éviter de copier-coller le code de parsing
                List<int> indexes = view.ParseSelection(input);

                foreach (int idx in indexes)
                {
                    view.GetViewModel().ExecuteJob(idx);
                }
                Console.WriteLine("Command Line Execution Finished.");
            }
            else
            {
                view.ShowMenu();
            }
        }
    }
}