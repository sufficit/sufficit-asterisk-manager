using Sufficit.Asterisk.Manager.Response;
using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    ///     The GetConfigAction sends a GetConfig command to the asterisk server.
    ///     Dumps contents of a configuration file, with optional filtering by category or variable matches.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: GetConfig
    /// Purpose: Retrieve configuration file contents
    /// Privilege Required: system,config,all
    /// 
    /// Available since: Asterisk 1.4.0
    /// 
    /// Response Flow:
    /// 1. Response: Success/Error
    /// 2. Multiple events with configuration data:
    ///    - Category: Configuration section name
    ///    - Var: Variable name
    ///    - Value: Variable value
    /// 
    /// Filtering Options:
    /// - Category: Limit to specific configuration section
    /// - Filter: Advanced filtering with name_regex=value_regex expressions
    /// 
    /// Special Filter Features:
    /// - TEMPLATES filter can be:
    ///   * "include" - Include template sections
    ///   * "restrict" - Only show template sections
    ///   * Default behavior excludes templates
    /// 
    /// Filter Format:
    /// Comma-separated list of name_regex=value_regex expressions
    /// Example: "type=peer,qualify=yes"
    /// 
    /// Usage Scenarios:
    /// - Configuration backup and validation
    /// - Automated configuration auditing
    /// - Template and inheritance analysis
    /// - Dynamic configuration reading
    /// - Troubleshooting configuration issues
    /// - Configuration documentation generation
    /// 
    /// Security Considerations:
    /// - May expose sensitive configuration data
    /// - Requires appropriate manager privileges
    /// - Should be restricted to authorized users only
    /// - Consider filtering sensitive sections
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Large configuration files can generate numerous events.
    /// Use Category filter to limit scope when possible.
    /// 
    /// Example Usage:
    /// <code>
    /// // Get entire sip.conf
    /// var config = new GetConfigAction("sip.conf");
    /// 
    /// // Get specific category
    /// var config = new GetConfigAction("sip.conf") { Category = "general" };
    /// 
    /// // Filter for peer templates
    /// var config = new GetConfigAction("sip.conf") 
    /// { 
    ///     Filter = "type=peer,TEMPLATES=include" 
    /// };
    /// </code>
    /// </remarks>
    /// <seealso cref="GetConfigResponse"/>
    public class GetConfigAction : ManagerActionResponse
    {
        /// <summary>
        ///     Creates a new GetConfigAction.
        /// </summary>
        public GetConfigAction()
        {
        }

        /// <summary>
        ///     Creates a new GetConfigAction for a specific configuration file.
        /// </summary>
        /// <param name="filename">The configuration filename (e.g., "sip.conf", "extensions.conf")</param>
        /// <remarks>
        /// Common configuration files:
        /// - "sip.conf" - SIP channel configuration
        /// - "pjsip.conf" - PJSIP configuration  
        /// - "extensions.conf" - Dialplan configuration
        /// - "queues.conf" - Call queue configuration
        /// - "voicemail.conf" - Voicemail configuration
        /// - "manager.conf" - Manager interface configuration
        /// - "asterisk.conf" - Core Asterisk settings
        /// </remarks>
        public GetConfigAction(string filename)
        {
            Filename = filename;
        }

        /// <summary>
        ///     Get the name of this action.
        /// </summary>
        public override string Action
            => "GetConfig";

        /// <summary>
        ///     Gets or sets the configuration filename.
        /// </summary>
        /// <value>
        ///     Configuration filename (e.g. "sip.conf", "extensions.conf").
        /// </value>
        /// <remarks>
        /// File requirements:
        /// - Must be a valid Asterisk configuration file
        /// - File must exist in Asterisk's configuration directory
        /// - File must be readable by Asterisk process
        /// - Include .conf extension for standard config files
        /// 
        /// The filename is relative to Asterisk's configuration directory
        /// (typically /etc/asterisk).
        /// </remarks>
        public string Filename { get; set; } = default!;

        /// <summary>
        ///     Gets or sets the specific category within the configuration file.
        /// </summary>
        /// <value>
        ///     Category name to filter by, or null to retrieve all categories.
        /// </value>
        /// <remarks>
        /// Category examples:
        /// - "general" - General settings section
        /// - "authentication" - Authentication settings
        /// - "1001" - Specific peer/endpoint definition
        /// - "default" - Default context in extensions.conf
        /// 
        /// When specified, only the named category is returned.
        /// This significantly reduces the response size for large files.
        /// Category names are case-sensitive.
        /// </remarks>
        public string? Category { get; set; }

        /// <summary>
        ///     Gets or sets the filter criteria for advanced configuration filtering.
        /// </summary>
        /// <value>
        ///     Comma-separated list of name_regex=value_regex expressions.
        /// </value>
        /// <remarks>
        /// Filter format:
        /// "name_regex=value_regex,name_regex=value_regex,..."
        /// 
        /// Examples:
        /// - "type=peer" - Only variables named "type" with value "peer"
        /// - "qualify=yes,type=peer" - Peers with qualify enabled
        /// - "context=.*internal.*" - Variables with context containing "internal"
        /// 
        /// Special TEMPLATES variable:
        /// - "TEMPLATES=include" - Include template sections
        /// - "TEMPLATES=restrict" - Only template sections
        /// - Default behavior excludes templates
        /// 
        /// Regular expressions:
        /// - Both name and value support regex patterns
        /// - Use .* for wildcard matching
        /// - Escape special regex characters as needed
        /// </remarks>
        public string? Filter { get; set; }

        /// <summary>
        ///     Returns the response type for this action.
        /// </summary>
        /// <returns>The Type of GetConfigResponse</returns>
        public override Type ActionCompleteResponseClass()
            => typeof(GetConfigResponse);
    }
}