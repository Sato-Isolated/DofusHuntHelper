using System.Diagnostics;
using System.Windows.Input;

namespace DofusHuntHelper.Commands
{
    /// <summary>
    /// Implémentation générique de <see cref="ICommand"/> permettant
    /// de relayer les actions et la logique de validation (CanExecute).
    /// </summary>
    public class RelayCommand : ICommand
    {
        /// <summary>
        /// Représente une fonction booléenne déterminant si la commande peut s’exécuter.
        /// </summary>
        private readonly Predicate<object?>? _canExecute;

        /// <summary>
        /// Représente l’action à exécuter lorsque la commande est invoquée.
        /// </summary>
        private readonly Action<object?> _execute;

        /// <summary>
        /// Se déclenche lorsque la condition <see cref="CanExecute(object?)"/> est modifiée.
        /// Cet événement permet à WPF (ou d’autres frameworks) de réactualiser l’état de la commande.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="RelayCommand"/>.
        /// </summary>
        /// <param name="execute">Délégué (action) à exécuter lorsque la commande est invoquée.</param>
        /// <param name="canExecute">
        /// Fonction optionnelle qui détermine si la commande peut s’exécuter.
        /// Si elle est nulle, la commande est toujours exécutable.
        /// </param>
        /// <exception cref="ArgumentNullException">Lève une exception si <paramref name="execute"/> est nul.</exception>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            if (execute == null)
            {
                Debug.Print("[RelayCommand] Le delegate 'execute' est null. Levée d'ArgumentNullException.");
                throw new ArgumentNullException(nameof(execute));
            }

            _execute = execute;
            _canExecute = canExecute;

            Debug.Print("[RelayCommand] Constructeur appelé. _execute non-null, _canExecute={0}",
                _canExecute != null);
        }

        /// <summary>
        /// Détermine si la commande peut s’exécuter en fonction du paramètre spécifié.
        /// </summary>
        /// <param name="parameter">Paramètre (éventuellement null) passé à la commande.</param>
        /// <returns>
        /// <c>true</c> si la commande est exécutable (soit parce que <paramref name="_canExecute"/> est null,
        /// soit parce qu’elle retourne true), <c>false</c> sinon.
        /// </returns>
        public bool CanExecute(object? parameter)
        {
            var result = _canExecute == null || _canExecute(parameter);
            Debug.Print("[RelayCommand] CanExecute({0}) -> {1}", parameter, result);
            return result;
        }

        /// <summary>
        /// Exécute la commande en appelant l’action déléguée <see cref="_execute"/>.
        /// </summary>
        /// <param name="parameter">Paramètre (éventuellement null) passé à la commande.</param>
        public void Execute(object? parameter)
        {
            Debug.Print("[RelayCommand] Execute({0}) appelé.", parameter);
            _execute(parameter);
        }
    }
}