using System;

namespace EasySave.Views
{
    public class ConsoleView
    {
        // Stores the selected language (FR or EN)
        public string CurrentLanguage { get; private set; }

        // Displays the initial language selection menu
        public void ChooseLanguage()
        {
            Console.WriteLine("Choisissez votre langue / Choose your language :");
            Console.WriteLine("1. Français");
            Console.WriteLine("2. English");

            string choice = Console.ReadLine();

            if (choice == "2")
            {
                CurrentLanguage = "EN";
            }
            else
            {
                CurrentLanguage = "FR"; // Default to French if input is invalid
            }

            Console.Clear();
        }

        // Displays the correct string based on the selected language
        public void ShowMessage(string messageFR, string messageEN)
        {
            if (CurrentLanguage == "FR")
            {
                Console.WriteLine(messageFR);
            }
            else
            {
                Console.WriteLine(messageEN);
            }
        }

        // Waits for user input from the keyboard
        public string GetUserInput()
        {
            Console.Write("> ");
            return Console.ReadLine();
        }
    }
}