using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Linq;

namespace Sufficit.Asterisk.Manager
{   
    /// <summary>
    /// Handles subscription and event dispatch for a specific Asterisk Manager Interface (AMI) event type.
    /// Each instance manages handlers for one specific AMI event type (T).
    /// </summary>
    /// <typeparam name="T">The specific type of Asterisk Manager Interface event this subscription handles</typeparam>
    public class ManagerEventSubscription<T> : ManagerInvokable where T : IManagerEvent
    {
        private event EventHandler<T>? Handler;

        // Implementação das propriedades e métodos abstratos da classe base.
        public override Type Type => typeof(T);
        public override int Count => Handler?.GetInvocationList().Length ?? 0;

        // O construtor agora chama o construtor da classe base para definir a Key.
        public ManagerEventSubscription(string key) : base(key) { }

        /// <summary>
        /// Invoca os assinantes de forma segura, garantindo que o tipo do evento corresponde.
        /// </summary>
        public override void Invoke(object? sender, IManagerEvent e)
        {
            // Adicionada verificação de tipo para evitar InvalidCastException em tempo de execução.
            if (e is T specificEvent)
            {
                Handler?.Invoke(sender, specificEvent);
            }
        }

        /// <summary>
        /// Adiciona um assinante (delegate/action) e notifica que a contagem mudou.
        /// </summary>
        public void Add(EventHandler<T> action)
        {
            Handler += action;
            RaiseOnChanged(); // Chama o método protegido da classe base.
        }

        /// <summary>
        /// Remove um assinante e notifica que a contagem mudou.
        /// </summary>
        public void Remove(EventHandler<T> action)
        {
            Handler -= action;
            RaiseOnChanged(); // Chama o método protegido da classe base.
        }
    }    
}