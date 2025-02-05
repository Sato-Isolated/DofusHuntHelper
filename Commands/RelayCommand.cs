using System.Diagnostics;
using System.Windows.Input;
using Serilog;

namespace DofusHuntHelper.Commands;

public class RelayCommand : ICommand
{
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<object?> _execute;
    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        if (execute == null)
        {
            Log.Error("[RelayCommand] Le delegate 'execute' est null. Levée d'ArgumentNullException.");
            throw new ArgumentNullException(nameof(execute));
        }

        _execute = execute;
        _canExecute = canExecute;

        Debug.Print("[RelayCommand] Constructeur appelé. _execute non-null, _canExecute={0}",
            _canExecute != null);
    }

    public bool CanExecute(object? parameter)
    {
        var result = _canExecute == null || _canExecute(parameter);
        Debug.Print("[RelayCommand] CanExecute({0}) -> {1}", parameter, result);
        return result;
    }

    public void Execute(object? parameter)
    {
        Debug.Print("[RelayCommand] Execute({0}) appelé.", parameter);
        _execute(parameter);
    }
}