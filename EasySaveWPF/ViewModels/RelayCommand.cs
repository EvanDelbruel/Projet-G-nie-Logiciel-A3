using System;
using System.Windows.Input;

namespace EasySaveWPF.ViewModels
{
    // Implements the ICommand interface to route commands from the WPF UI to methods in the ViewModel
    public class RelayCommand : ICommand
    {
        // Delegate representing the method to execute when the command is invoked
        private readonly Action<object?> _execute;

        // Optional delegate evaluating whether the command is allowed to execute in the current state
        private readonly Predicate<object?>? _canExecute;

        // Initializes a new instance of the command with the required execution logic and an optional condition
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Hooks into the WPF CommandManager to automatically trigger UI updates when conditions change
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Evaluates the condition predicate, or defaults to true if no condition was provided
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        // Invokes the encapsulated execution logic
        public void Execute(object? parameter) => _execute(parameter);
    }
}