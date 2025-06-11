using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// Classe auxiliar que implementa IDisposable para cancelar a inscrição de um evento facilmente.
    /// </summary>
    public class DisposableHandler<T> : IDisposable where T : IManagerEvent
    {
        private readonly ManagerEventHandler<T> _control;
        private readonly EventHandler<T> _action;

        public DisposableHandler(ManagerEventHandler<T> control, EventHandler<T> action)
        {
            _control = control;
            _action = action;

            // Corrigido para usar o método "Add" em vez de "Attach".
            _control.Add(action);
        }

        public void Dispose()
        {
            // Corrigido para usar o método "Remove" em vez de "Dettach".
            _control.Remove(_action);
        }
    }
}
