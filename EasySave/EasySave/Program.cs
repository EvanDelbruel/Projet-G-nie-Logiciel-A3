using System;
using EasySave.Controllers;

namespace EasySave
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainController app = new MainController();
            app.Start(args); // On lance le contrôleur en lui passant les arguments éventuels
        }
    }
}