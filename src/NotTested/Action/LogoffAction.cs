using Sufficit.Asterisk.Manager.Action;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     The LogoffAction causes the server to close the connection.
    /// </summary>
    public class LogOffAction : ManagerAction
    {
        /// <summary>
        ///     Get the name of this action, i.e. "Logoff".
        /// </summary>
        public override string Action => "Logoff";
    }
}