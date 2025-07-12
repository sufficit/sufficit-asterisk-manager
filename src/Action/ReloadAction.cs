using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ReloadAction reloads Asterisk modules or configuration files.
    /// This action allows dynamic reconfiguration without restarting Asterisk.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Reload
    /// Purpose: Reload modules or configuration files
    /// Privilege Required: system,config,all
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - Module: The module or configuration file to reload (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Module Types:
    /// - Configuration files: "extensions.conf", "sip.conf", "iax.conf"
    /// - Module names: "app_queue", "chan_sip", "res_musiconhold"
    /// - Special keywords: "all" (reload all), "dialplan" (extensions only)
    /// 
    /// Common Reload Targets:
    /// - "dialplan": Reload extensions.conf (most common)
    /// - "sip.conf": Reload SIP configuration
    /// - "queues.conf": Reload queue configuration
    /// - "voicemail.conf": Reload voicemail settings
    /// - "manager.conf": Reload manager interface settings
    /// - "musiconhold.conf": Reload music on hold classes
    /// - "features.conf": Reload call features
    /// - "cdr.conf": Reload CDR settings
    /// 
    /// Reload Behavior:
    /// - Configuration changes take effect immediately
    /// - Active calls are not disrupted (usually)
    /// - New calls use updated configuration
    /// - Some modules may have specific reload behaviors
    /// - Module dependencies are handled automatically
    /// 
    /// Usage Scenarios:
    /// - Apply configuration changes without restart
    /// - Update dialplan after modifications
    /// - Refresh SIP peer configurations
    /// - Update queue member assignments
    /// - Apply new feature configurations
    /// - Refresh security settings
    /// 
    /// Impact Considerations:
    /// - Dialplan reload: Affects new call routing only
    /// - SIP reload: May temporarily affect registration
    /// - Queue reload: May affect agent assignments
    /// - CDR reload: May affect call recording
    /// - Manager reload: May affect current connections
    /// 
    /// Best Practices:
    /// - Test configuration before reloading
    /// - Use specific module names when possible
    /// - Avoid "all" reload in production
    /// - Monitor logs for reload errors
    /// - Schedule reloads during low traffic
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - Module support varies by version
    /// - Some modules added reload support later
    /// - Modern versions have better reload handling
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Reload operations are executed in the main Asterisk thread.
    /// Some modules may require full restart for certain changes.
    /// Error messages are logged to Asterisk console and logs.
    /// 
    /// Security Considerations:
    /// - Requires system-level privileges
    /// - Can affect system-wide configuration
    /// - Should be restricted to authorized users
    /// - Monitor for unauthorized reload attempts
    /// - Validate configuration before reloading
    /// 
    /// Error Conditions:
    /// - Invalid module name
    /// - Configuration syntax errors
    /// - Module not loaded
    /// - Insufficient privileges
    /// - File system permissions
    /// 
    /// Example Usage:
    /// <code>
    /// // Reload dialplan
    /// var reload = new ReloadAction("dialplan");
    /// 
    /// // Reload SIP configuration
    /// var reload = new ReloadAction("chan_sip");
    /// 
    /// // Reload specific configuration file
    /// var reload = new ReloadAction("queues.conf");
    /// </code>
    /// </remarks>
    /// <seealso cref="CommandAction"/>
    public class ReloadAction : ManagerAction
    {
        /// <summary>
        /// Creates a new ReloadAction for the specified module.
        /// </summary>
        /// <param name="module">The module or configuration file to reload (Required)</param>
        /// <remarks>
        /// Module Parameter Guidelines:
        /// 
        /// Configuration Files:
        /// - Use file name: "extensions.conf", "sip.conf"
        /// - Include .conf extension for clarity
        /// - File must exist in Asterisk config directory
        /// 
        /// Module Names:
        /// - Use module name: "chan_sip", "app_queue"
        /// - Check "module show" output for exact names
        /// - Module must support dynamic reload
        /// 
        /// Special Keywords:
        /// - "dialplan": Reload extensions.conf only
        /// - "all": Reload all modules (use with caution)
        /// 
        /// Common Examples:
        /// - "dialplan": Most frequently used
        /// - "chan_sip": SIP channel driver
        /// - "app_queue": Queue application
        /// - "res_musiconhold": Music on hold
        /// - "cdr": Call detail records
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when module is null</exception>
        /// <exception cref="ArgumentException">Thrown when module is empty</exception>
        public ReloadAction(string module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));
            if (string.IsNullOrWhiteSpace(module))
                throw new ArgumentException("Module cannot be empty", nameof(module));

            Module = module;
        }

        /// <summary>
        /// Creates a new ReloadAction with module and action ID.
        /// </summary>
        /// <param name="module">The module or configuration file to reload (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of the response with this specific request.
        /// Useful for tracking the success/failure of reload operations.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when module is null</exception>
        /// <exception cref="ArgumentException">Thrown when module is empty</exception>
        public ReloadAction(string module, string actionId) : this(module)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Reload"</value>
        public override string Action => "Reload";

        /// <summary>
        /// Gets the name of the module or configuration file to reload.
        /// This property is required and read-only after construction.
        /// </summary>
        /// <value>
        /// The module name or configuration file to reload.
        /// </value>
        /// <remarks>
        /// Module Categories:
        /// 
        /// Core Modules:
        /// - "dialplan": Extensions and routing configuration
        /// - "manager": Manager interface settings
        /// - "logger": Logging configuration
        /// - "cli": Command line interface settings
        /// 
        /// Channel Drivers:
        /// - "chan_sip": SIP channel driver
        /// - "chan_iax2": IAX2 channel driver
        /// - "chan_dahdi": DAHDI channel driver
        /// - "chan_pjsip": PJSIP channel driver (Asterisk 12+)
        /// 
        /// Applications:
        /// - "app_queue": Call queue application
        /// - "app_voicemail": Voicemail application
        /// - "app_meetme": Conference application
        /// - "app_dial": Dial application
        /// 
        /// Resources:
        /// - "res_musiconhold": Music on hold
        /// - "res_features": Call features
        /// - "res_parking": Call parking
        /// - "res_agi": AGI resource
        /// 
        /// Configuration Files:
        /// - "extensions.conf": Dialplan configuration
        /// - "sip.conf": SIP configuration
        /// - "queues.conf": Queue configuration
        /// - "voicemail.conf": Voicemail configuration
        /// - "features.conf": Call features configuration
        /// - "musiconhold.conf": Music on hold configuration
        /// - "cdr.conf": CDR configuration
        /// 
        /// Reload Impact:
        /// - "dialplan": Updates call routing immediately
        /// - "chan_sip": May cause brief registration interruption
        /// - "app_queue": Updates queue configuration and members
        /// - "res_musiconhold": Updates hold music classes
        /// - "manager": May affect current manager connections
        /// 
        /// Special Considerations:
        /// - Some modules may not support hot reload
        /// - Configuration errors prevent successful reload
        /// - Active calls typically continue unaffected
        /// - New calls use updated configuration
        /// </remarks>
        public string Module { get; }
    }
}