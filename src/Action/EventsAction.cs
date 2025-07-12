using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The EventsAction controls the flow of events from Asterisk to the manager client.
    /// This action enables or disables sending of events based on specified event categories.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Events
    /// Purpose: Control which events are sent to the manager client
    /// Privilege Required: None (affects only current connection)
    /// Available since: Asterisk 0.9.0
    /// 
    /// Required Parameters:
    /// - EventMask: Controls which events are sent (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Event Mask Values:
    /// - "on": Enable all events
    /// - "off": Disable all events  
    /// - Comma-separated categories: "system,call,log"
    /// 
    /// Event Categories:
    /// - "system": System-related events (startup, shutdown, module load)
    /// - "call": Call-related events (dial, hangup, bridge)
    /// - "log": Log message events (warnings, errors, debug)
    /// - "verbose": Verbose message events
    /// - "command": Command response events
    /// - "agent": Call center agent events
    /// - "user": User-defined events
    /// - "config": Configuration change events
    /// - "dtmf": DTMF digit events
    /// - "reporting": Reporting and statistics events
    /// - "cdr": Call Detail Record events
    /// - "dialplan": Dialplan execution events
    /// - "originate": Origination-related events
    /// - "agi": AGI script events
    /// - "cc": Call completion events
    /// - "aoc": Advice of Charge events
    /// - "test": Test framework events
    /// - "security": Security-related events
    /// 
    /// Usage Scenarios:
    /// - Performance optimization: Disable unnecessary events
    /// - Targeted monitoring: Enable only relevant event types
    /// - Bandwidth conservation: Reduce network traffic
    /// - Application-specific filtering: Match events to app needs
    /// - Debug sessions: Enable verbose/system events temporarily
    /// - Production monitoring: Focus on call/system events
    /// 
    /// Performance Considerations:
    /// - "on": Maximum events, highest bandwidth usage
    /// - "off": No events, minimal bandwidth
    /// - Selective categories: Balanced approach
    /// - Event volume varies greatly by category
    /// - System load affects event generation rate
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 0.9.0
    /// - Event categories expanded over time
    /// - Some categories version-specific
    /// - Modern versions support more granular control
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Changes apply only to the current manager connection.
    /// Default state is typically "off" for new connections.
    /// Login action can also set initial event mask.
    /// 
    /// Security Considerations:
    /// - Events may contain sensitive information
    /// - Some categories reveal system internals
    /// - Consider authorization for event categories
    /// - Monitor bandwidth usage with "on" setting
    /// 
    /// Example Usage:
    /// <code>
    /// // Enable all events
    /// var events = new EventsAction("on");
    /// 
    /// // Disable all events
    /// var events = new EventsAction("off");
    /// 
    /// // Enable only call and system events
    /// var events = new EventsAction("call,system");
    /// 
    /// // Enable with action ID for correlation
    /// var events = new EventsAction("call,system", "evt_001");
    /// </code>
    /// </remarks>
    /// <seealso cref="LoginAction"/>
    public class EventsAction : ManagerAction
    {
        /// <summary>
        /// Creates a new EventsAction with the specified event mask.
        /// </summary>
        /// <param name="eventMask">Event mask controlling which events are sent (Required)</param>
        /// <remarks>
        /// Event Mask Examples:
        /// - "on": Enable all events (high bandwidth)
        /// - "off": Disable all events (minimal bandwidth)
        /// - "call": Only call-related events
        /// - "system,call": System and call events
        /// - "call,log,verbose": Multiple specific categories
        /// 
        /// Common Combinations:
        /// - "call,system": Essential monitoring
        /// - "call,dtmf": Call control applications
        /// - "system,log": System administration
        /// - "call,dialplan": Call flow debugging
        /// </remarks>
        public EventsAction(string eventMask)
        {
            EventMask = eventMask ?? throw new ArgumentNullException(nameof(eventMask));
        }

        /// <summary>
        /// Creates a new EventsAction with the specified event mask and action ID.
        /// </summary>
        /// <param name="eventMask">Event mask controlling which events are sent (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of the response with this specific request.
        /// This is useful when multiple Events actions might be sent concurrently
        /// or when tracking the success/failure of event mask changes.
        /// </remarks>
        public EventsAction(string eventMask, string actionId) : this(eventMask)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Events"</value>
        public override string Action => "Events";

        /// <summary>
        /// Gets or sets the event mask that controls which events are sent.
        /// This property is required.
        /// </summary>
        /// <value>
        /// Event mask string specifying which events to enable.
        /// </value>
        /// <remarks>
        /// Event Mask Syntax:
        /// - Single value: "on", "off", or category name
        /// - Multiple categories: Comma-separated list
        /// - No spaces around commas: "call,system" not "call, system"
        /// - Case-sensitive category names
        /// 
        /// Event Mask Values:
        /// 
        /// All Events:
        /// - "on": Enable all event categories
        /// - "off": Disable all event categories
        /// 
        /// System Events ("system"):
        /// - ModuleLoadReport: Module loading status
        /// - Reload: Configuration reload notifications
        /// - Shutdown: System shutdown notifications
        /// - FullyBooted: System startup completion
        /// - CertificateMap: TLS certificate events
        /// 
        /// Call Events ("call"):
        /// - Newchannel: New channel creation
        /// - Hangup: Channel termination
        /// - NewState: Channel state changes
        /// - Dial: Outbound call attempts
        /// - Bridge: Channel bridging events
        /// - Transfer: Call transfer events
        /// - Hold: Call hold/unhold events
        /// - MusicOnHold: Music on hold events
        /// 
        /// Log Events ("log"):
        /// - LogChannel: Log message events
        /// - WARNING, ERROR, NOTICE level messages
        /// - Module-specific log entries
        /// 
        /// DTMF Events ("dtmf"):
        /// - DTMFBegin: DTMF tone start
        /// - DTMFEnd: DTMF tone end
        /// - FlashEvent: Hook flash detection
        /// 
        /// Configuration Events ("config"):
        /// - ConfigChange: Configuration file changes
        /// - ReloadResponse: Reload command responses
        /// 
        /// Agent Events ("agent"):
        /// - AgentCalled: Agent called by system
        /// - AgentConnect: Agent connected to caller
        /// - AgentComplete: Agent call completed
        /// - AgentLogin: Agent login events
        /// - AgentLogoff: Agent logout events
        /// 
        /// User Events ("user"):
        /// - UserEvent: Custom application events
        /// - Application-defined event data
        /// 
        /// CDR Events ("cdr"):
        /// - Cdr: Call Detail Records
        /// - Call billing and duration information
        /// 
        /// Reporting Events ("reporting"):
        /// - Statistics and performance data
        /// - Queue statistics
        /// - Channel counts
        /// 
        /// AGI Events ("agi"):
        /// - AGIExec: AGI command execution
        /// - AGI script interactions
        /// 
        /// Dialplan Events ("dialplan"):
        /// - NewExten: Dialplan extension changes
        /// - VarSet: Variable assignments
        /// - Application execution tracking
        /// 
        /// Originate Events ("originate"):
        /// - OriginateResponse: Origination results
        /// - Related to Originate action responses
        /// 
        /// Command Events ("command"):
        /// - CommandResponse: CLI command responses
        /// - Command completion status
        /// 
        /// Security Events ("security"):
        /// - Authentication attempts
        /// - Security-related notifications
        /// - Failed login attempts
        /// 
        /// Performance Impact:
        /// - "on": High CPU and network usage
        /// - "call": Moderate usage (most common)
        /// - "system": Low usage
        /// - "log,verbose": High usage (debug scenarios)
        /// - Multiple categories: Cumulative impact
        /// 
        /// Best Practices:
        /// - Start with minimal set: "call,system"
        /// - Add categories as needed for specific features
        /// - Use "off" when events not needed
        /// - Monitor bandwidth with "on" in production
        /// - Consider application requirements vs. performance
        /// </remarks>
        public string EventMask { get; set; }
    }
}