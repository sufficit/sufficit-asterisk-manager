using Sufficit.Asterisk.Manager.Response;
using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The CommandAction sends a command line interface (CLI) command to the Asterisk server.
    /// This action allows execution of any CLI command that would be available on the Asterisk console.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Command
    /// Purpose: Execute Asterisk CLI commands remotely
    /// Privilege Required: command,all
    /// 
    /// Required Parameters:
    /// - Command: The CLI command to execute (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Command Types:
    /// - Show commands: Display system information
    /// - Core commands: Core system operations
    /// - Module commands: Module management
    /// - Channel commands: Channel operations
    /// - Database commands: Asterisk database operations
    /// - Dialplan commands: Dialplan operations
    /// 
    /// Common Commands:
    /// - "core show channels": Display active channels
    /// - "sip show peers": Show SIP peer status
    /// - "queue show": Display queue information
    /// - "core reload": Reload configuration
    /// - "module show": List loaded modules
    /// - "database show": Show database contents
    /// - "dialplan show": Display dialplan
    /// - "core set verbose": Set verbosity level
    /// - "core set debug": Set debug level
    /// 
    /// Response Format:
    /// - Response contains command output
    /// - Multi-line output preserved
    /// - Error messages included in response
    /// - Command completion status indicated
    /// 
    /// Usage Scenarios:
    /// - Remote system administration
    /// - Automated monitoring scripts
    /// - Configuration verification
    /// - Troubleshooting and diagnostics
    /// - System health checks
    /// - Real-time system queries
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - Command set varies by Asterisk version
    /// - Some commands added/removed over time
    /// - Modern versions have extensive CLI
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Commands execute with Asterisk process privileges.
    /// Output formatting depends on command type.
    /// Some commands may take time to complete.
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Can execute powerful system commands
    /// - Should be restricted to trusted users
    /// - Consider command validation and filtering
    /// - Monitor for privilege escalation attempts
    /// 
    /// Error Conditions:
    /// - Invalid command syntax
    /// - Unknown command
    /// - Insufficient privileges for command
    /// - Command execution failure
    /// - System resource limitations
    /// 
    /// Example Usage:
    /// <code>
    /// // Show active channels
    /// var command = new CommandAction("core show channels");
    /// 
    /// // Show SIP peers
    /// var command = new CommandAction("sip show peers");
    /// 
    /// // Reload configuration
    /// var command = new CommandAction("core reload");
    /// 
    /// // Show queue status
    /// var command = new CommandAction("queue show");
    /// </code>
    /// </remarks>
    /// <seealso cref="CommandResponse"/>
    public class CommandAction : ManagerActionResponse
    {
        /// <summary>
        /// Creates a new CommandAction with the given command.
        /// </summary>
        /// <param name="command">The CLI command to execute (Required)</param>
        /// <remarks>
        /// Command Requirements:
        /// - Must be a valid Asterisk CLI command
        /// - Command syntax must be correct
        /// - User must have appropriate privileges for the command
        /// 
        /// Command Examples:
        /// - "core show channels": Display channel information
        /// - "sip show registry": Show SIP registrations
        /// - "queue show": Display queue statistics
        /// - "module show": List loaded modules
        /// - "core set verbose 3": Set verbosity level
        /// 
        /// The command will be executed exactly as provided.
        /// Include all necessary parameters and options.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when command is null</exception>
        /// <exception cref="ArgumentException">Thrown when command is empty</exception>
        public CommandAction(string command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be empty", nameof(command));

            Command = command;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Command"</value>
        public override string Action => "Command";

        /// <summary>
        /// Gets the CLI command to send to the Asterisk server.
        /// This property is required and read-only after construction.
        /// </summary>
        /// <value>
        /// The command line interface command to execute.
        /// </value>
        /// <remarks>
        /// Command Categories:
        /// 
        /// Core Commands:
        /// - "core show channels": Active channel information
        /// - "core show uptime": System uptime and statistics
        /// - "core show version": Asterisk version information
        /// - "core reload": Reload all configuration files
        /// - "core restart gracefully": Graceful system restart
        /// - "core stop gracefully": Graceful system shutdown
        /// - "core set verbose [level]": Set console verbosity
        /// - "core set debug [level]": Set debug verbosity
        /// 
        /// Channel Commands:
        /// - "channel show [channel]": Specific channel details
        /// - "soft hangup [channel]": Soft hangup on channel
        /// - "channel originate [params]": Originate a call
        /// 
        /// SIP Commands:
        /// - "sip show peers": SIP peer status and configuration
        /// - "sip show registry": SIP registration status
        /// - "sip show channels": Active SIP channels
        /// - "sip show users": SIP user configurations
        /// - "sip reload": Reload SIP configuration
        /// 
        /// PJSIP Commands (Asterisk 12+):
        /// - "pjsip show endpoints": PJSIP endpoint status
        /// - "pjsip show registrations": PJSIP registration status
        /// - "pjsip show channels": Active PJSIP channels
        /// - "pjsip reload": Reload PJSIP configuration
        /// 
        /// IAX2 Commands:
        /// - "iax2 show peers": IAX2 peer status
        /// - "iax2 show registry": IAX2 registration status
        /// - "iax2 show channels": Active IAX2 channels
        /// 
        /// Queue Commands:
        /// - "queue show": All queue statistics
        /// - "queue show [queue]": Specific queue details
        /// - "queue add member [member] to [queue]": Add queue member
        /// - "queue remove member [member] from [queue]": Remove member
        /// - "queue pause member [member]": Pause queue member
        /// - "queue unpause member [member]": Unpause queue member
        /// 
        /// Database Commands:
        /// - "database show": Show all database entries
        /// - "database show [family]": Show family entries
        /// - "database put [family] [key] [value]": Add database entry
        /// - "database del [family] [key]": Delete database entry
        /// - "database deltree [family]": Delete family tree
        /// 
        /// Dialplan Commands:
        /// - "dialplan show": Show entire dialplan
        /// - "dialplan show [context]": Show specific context
        /// - "dialplan reload": Reload dialplan
        /// 
        /// Module Commands:
        /// - "module show": List all loaded modules
        /// - "module show like [pattern]": Filter module list
        /// - "module load [module]": Load specific module
        /// - "module unload [module]": Unload specific module
        /// - "module reload [module]": Reload specific module
        /// 
        /// Voicemail Commands:
        /// - "voicemail show users": Show voicemail users
        /// - "voicemail show zones": Show voicemail zones
        /// - "voicemail reload": Reload voicemail configuration
        /// 
        /// Conference Commands:
        /// - "confbridge list": List active conferences
        /// - "confbridge list [conference]": Show conference details
        /// - "confbridge kick [conference] [participant]": Remove participant
        /// 
        /// Call Detail Record Commands:
        /// - "cdr show status": CDR system status
        /// - "cdr submit": Force CDR submission
        /// 
        /// The command is executed with the same privileges as the Asterisk process.
        /// Some commands may require additional module loading or configuration.
        /// </remarks>
        public string Command { get; }

        /// <summary>
        /// Returns the response type for this action.
        /// </summary>
        /// <returns>The Type of CommandResponse</returns>
        public override Type ActionCompleteResponseClass()
            => typeof(CommandResponse);
    }
}