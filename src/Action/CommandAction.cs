namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    ///     The CommandAction sends a command line interface (CLI) command to the asterisk server.<br />
    ///     For a list of supported commands type help on asterisk's command line.
    /// </summary>
    public class CommandAction : ManagerAction
    {
        /// <summary>
        ///     Creates a new CommandAction with the given command.
        /// </summary>
        /// <param name="command">the CLI command to execute.</param>
        public CommandAction(string command) => Command = command;

        /// <summary>
        ///     Get the name of this action, i.e. "Command".
        /// </summary>
        public override string Action
        {
            get { return "Command"; }
        }

        /// <summary>
        ///     Get/Set the CLI command to send to the asterisk server.
        /// </summary>
        public string Command { get; }
    }
}