using System.Windows;
using EasySaveWPF.Models;

namespace EasySaveWPF.Views
{
    public partial class AddJobWindow : Window
    {
        // Stores the newly created backup job upon successful validation and user confirmation
        public BackupJob? CreatedJob { get; private set; }

        public AddJobWindow(string currentLanguage)
        {
            InitializeComponent();

            // Applies basic UI localization based on the currently selected language
            if (currentLanguage == "English")
            {
                Title = "New Backup Job";
                LblName.Text = "Backup Name:";
                LblSource.Text = "Source Folder:";
                LblTarget.Text = "Target Folder:";
                LblType.Text = "Type:";
                BtnCancel.Content = "Cancel";
                BtnSave.Content = "Save";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validates that all required input fields are populated
            if (string.IsNullOrWhiteSpace(TxtName.Text) || string.IsNullOrWhiteSpace(TxtSource.Text) || string.IsNullOrWhiteSpace(TxtTarget.Text))
            {
                MessageBox.Show("Veuillez remplir tous les champs / Please fill all fields.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Instantiates the backup job model using the provided UI inputs
            CreatedJob = new BackupJob(
                TxtName.Text,
                TxtSource.Text,
                TxtTarget.Text,
                CmbType.Text
            );

            // Signals successful completion to the parent window and closes the dialog
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Aborts the operation and closes the dialog without saving
            DialogResult = false;
            Close();
        }
    }
}