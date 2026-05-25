using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Client.Helpers
{
    public class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private bool _isExecuting;

        public AsyncCommand(Func<Task> execute)
        {
            _execute = execute;
            _isExecuting = false;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return !_isExecuting;
        }

        public async void Execute(object parameter)
        {
            if (_isExecuting)
                return;

            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class AsyncCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private bool _isExecuting;

        public AsyncCommand(Func<T, Task> execute)
        {
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return !_isExecuting;
        }

        public async void Execute(object parameter)
        {
            if (_isExecuting)
                return;

            if (!(parameter is T typedParam))
                return;

            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await _execute(typedParam);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}