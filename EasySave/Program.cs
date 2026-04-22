using EasySave.Controllers;

namespace EasySave
{
    class Program
    {
        // Application entry point
        static void Main(string[] args)
        {
            MainController controller = new MainController();
            controller.Start(args);
        }
    }
}