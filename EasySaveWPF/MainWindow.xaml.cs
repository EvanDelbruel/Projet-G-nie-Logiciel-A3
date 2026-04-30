using EasySaveWPF.ViewModels;
using System.Windows;

namespace EasySaveWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Link the View to the ViewModel (DataBinding context)
            DataContext = new MainViewModel();
        }
    }
}