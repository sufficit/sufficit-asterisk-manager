using Sufficit.Asterisk.Manager.Action;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     The ZapDialOffhookAction dials a number on a zap channel while offhook.
    /// </summary>
    public class ZapDialOffhookAction : ManagerAction
    {
        /// <summary> Creates a new empty ZapDialOffhookAction.</summary>
        public ZapDialOffhookAction()
        {
        }

        /// <summary>
        ///     Creates a new ZapDialOffhookAction that dials the given number on the given zap channel.
        /// </summary>
        public ZapDialOffhookAction(int zapChannel, string number)
        {
            this.ZapChannel = zapChannel;
            this.Number = number;
        }

        /// <summary>
        ///     Get the name of this action, i.e. "ZapDialOffhook".
        /// </summary>
        public override string Action
        {
            get { return "ZapDialOffhook"; }
        }

        /// <summary>
        ///     Get/Set the number of the zap channel.<br />
        ///     This property is mandatory.
        /// </summary>
        public int ZapChannel { get; set; }

        /// <summary>
        ///     Get/Set the number to dial.<br />
        ///     This property is mandatory.
        /// </summary>
        public string Number { get; set; }
    }
}