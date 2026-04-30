using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasySaveWPF.ViewModels
{
    // Base class for ViewModels providing standard property change notification for WPF data binding
    public class ViewModelBase : INotifyPropertyChanged
    {
        // Event triggered when a property value changes, notifying the UI to refresh bound elements
        public event PropertyChangedEventHandler? PropertyChanged;

        // Invokes the PropertyChanged event. 
        // Uses CallerMemberName to automatically resolve and pass the calling property's name
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}