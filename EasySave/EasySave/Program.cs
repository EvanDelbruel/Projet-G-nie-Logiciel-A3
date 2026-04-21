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

            // Si l'utilisateur a tapé des arguments (ex: 1-3 ou 1;3)
            if (args.Length > 0)
            {
                string input = args[0];
                List<int> indexes = new List<int>();

                if (input.Contains("-"))
                {
                    string[] parts = input.Split('-');
                    for (int i = int.Parse(parts[0]); i <= int.Parse(parts[1]); i++) indexes.Add(i - 1);
                }
                else if (input.Contains(";"))
                {
                    foreach (var s in input.Split(';')) indexes.Add(int.Parse(s) - 1);
                }
                else
                {
                    indexes.Add(int.Parse(input) - 1);
                }

                foreach (int idx in indexes) view.GetViewModel().ExecuteJob(idx);
                Console.WriteLine("Command Line Execution Finished.");
            }
            else
            {
                view.ShowMenu();
            }
        }
    }
}