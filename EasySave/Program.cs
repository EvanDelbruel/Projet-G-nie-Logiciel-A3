using System;
using EasySave.Controllers; // pour le controleur

namespace EasySave
{
    class Program
    {
        static void Main(string[] args)
        {
            // creation du controleur principal
            ControleurPrincipal controleur = new ControleurPrincipal();

            // lancement en lui passant les arguments du terminal !
            controleur.Demarrer(args);
        }
    }
}