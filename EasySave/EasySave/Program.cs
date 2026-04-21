using EasySave.Views;

namespace EasySave
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // On lance notre Vue principale !
            MainView view = new MainView();
            view.ShowMenu();
        }
    }
}