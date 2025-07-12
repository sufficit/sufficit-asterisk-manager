using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ListCommandsAction requests a list of all available CLI commands.
    /// This action provides comprehensive information about commands available through the Asterisk CLI.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: ListCommands
    /// Purpose: List all available CLI commands
    /// Privilege Required: command,all
    /// Implementation: main/manager.c
    /// Available since: Asterisk 1.6
    /// 
    /// Required Parameters: None
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Response Information:
    /// Returns list of all CLI commands available in the current Asterisk instance.
    /// Each command includes description and usage information.
    /// 
    /// Command Categories:
    /// - Core commands (show, reload, etc.)
    /// - Module-specific commands
    /// - Application commands
    /// - Channel technology commands
    /// - Administrative commands
    /// - Debugging commands
    /// 
    /// Usage Scenarios:
    /// - Administrative tool development
    /// - CLI command discovery
    /// - Help system implementation
    /// - Automation script development
    /// - Documentation generation
    /// - Training and learning tools
    /// - System capability assessment
    /// - Module availability checking
    /// 
    /// Administrative Applications:
    /// - Build dynamic CLI interfaces
    /// - Create command validation systems
    /// - Implement command completion
    /// - Generate documentation
    /// - Develop training materials
    /// 
    /// Integration Applications:
    /// - Web-based Asterisk consoles
    /// - Mobile administration apps
    /// - Automated management systems
    /// - Configuration wizards
    /// - Monitoring dashboards
    /// 
    /// Command Information:
    /// Each command entry typically includes:
    /// - Command name and syntax
    /// - Brief description
    /// - Usage examples (in some versions)
    /// - Module association
    /// - Permission requirements
    /// 
    /// Dynamic Discovery:
    /// - Commands vary by loaded modules
    /// - Module loading changes available commands
    /// - Version differences affect command list
    /// - Configuration impacts command availability
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.6
    /// - Enhanced command information in newer versions
    /// - Improved categorization over time
    /// - Better module association in modern versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Command list reflects current system state.
    /// Results depend on loaded modules and configuration.
    /// Response format may vary between Asterisk versions.
    /// 
    /// Security Considerations:
    /// - Requires command privileges
    /// - May reveal system capabilities
    /// - Consider information disclosure
    /// - Useful for reconnaissance
    /// 
    /// Performance Considerations:
    /// - Lightweight operation
    /// - Command list is typically cached
    /// - Suitable for periodic updates
    /// - Minimal system impact
    /// 
    /// Example Usage:
    /// <code>
    /// // Get list of all available commands
    /// var listCommands = new ListCommandsAction();
    /// 
    /// // With action ID for tracking
    /// var trackedList = new ListCommandsAction();
    /// trackedList.ActionId = "commands_001";
    /// </code>
    /// </remarks>
    /// <seealso cref="CommandAction"/>
    public class ListCommandsAction : ManagerAction
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ListCommands"</value>
        public override string Action => "ListCommands";
    }
}