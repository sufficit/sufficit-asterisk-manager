using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ConfbridgeListAction requests information about active ConfBridge conferences.
    /// This action provides comprehensive conference monitoring and management capabilities.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: ConfbridgeList
    /// Purpose: List active ConfBridge conferences and participants
    /// Privilege Required: reporting,all
    /// Implementation: app_confbridge.c
    /// Available since: Asterisk 10.0
    /// 
    /// Required Parameters: None
    /// 
    /// Optional Parameters:
    /// - Conference: Specific conference name (Optional - all conferences if not specified)
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Response Flow:
    /// 1. Multiple ConfbridgeListEvent: One for each active conference
    /// 2. Single ConfbridgeListCompleteEvent: Indicates end of list
    /// 
    /// Conference Information:
    /// Each ConfbridgeListEvent contains:
    /// - Conference: Conference name/number
    /// - Parties: Number of participants
    /// - Marked: Number of marked participants
    /// - Locked: Whether conference is locked
    /// - Muted: Number of muted participants
    /// - Activity: Conference activity status
    /// 
    /// ConfBridge Overview:
    /// - Modern conference application
    /// - Replaces older MeetMe application
    /// - Advanced features and better performance
    /// - Support for various audio formats
    /// - Web-based management capabilities
    /// 
    /// Usage Scenarios:
    /// - Conference monitoring dashboards
    /// - Call center supervision
    /// - Meeting management systems
    /// - Capacity planning and reporting
    /// - Real-time conference status
    /// - Automated conference management
    /// - Web-based admin interfaces
    /// - Mobile conference apps
    /// 
    /// Conference Management:
    /// - Monitor active conferences
    /// - Track participant counts
    /// - Identify locked conferences
    /// - Manage conference capacity
    /// - Generate usage reports
    /// - Detect idle conferences
    /// 
    /// Business Applications:
    /// - Meeting room management
    /// - Conference call billing
    /// - Resource utilization tracking
    /// - Quality of service monitoring
    /// - Capacity planning
    /// - Cost center reporting
    /// 
    /// Real-time Monitoring:
    /// - Live conference counts
    /// - Participant tracking
    /// - Conference duration monitoring
    /// - Resource usage alerts
    /// - Automatic cleanup triggers
    /// 
    /// Integration Applications:
    /// - Web conference portals
    /// - Mobile meeting apps
    /// - Calendar integration
    /// - CRM conference tracking
    /// - Billing systems
    /// - Reporting dashboards
    /// 
    /// Asterisk Versions:
    /// - 10+: Basic ConfBridge functionality
    /// - 11+: Enhanced conference features
    /// - 12+: Improved manager events
    /// - 13+: Additional configuration options
    /// - Modern: Full feature support
    /// 
    /// ConfBridge Features:
    /// - Multiple audio formats
    /// - Video conferencing support
    /// - Recording capabilities
    /// - Advanced participant controls
    /// - Custom announcements
    /// - Web-based management
    /// 
    /// Performance Considerations:
    /// - Lightweight action with minimal overhead
    /// - Number of events depends on active conferences
    /// - Suitable for frequent monitoring
    /// - Consider event subscriptions for real-time updates
    /// 
    /// Implementation Notes:
    /// This action is implemented in app_confbridge.c in Asterisk source code.
    /// Results reflect current conference state at time of request.
    /// Conference status may change between request and response.
    /// Modern alternative to deprecated MeetMe application.
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Conference information may be sensitive
    /// - Consider access control for conference data
    /// - Monitor for unauthorized conference access
    /// 
    /// Example Usage:
    /// <code>
    /// // List all active conferences
    /// var confList = new ConfbridgeListAction();
    /// 
    /// // List specific conference
    /// var specificConf = new ConfbridgeListAction();
    /// specificConf.Conference = "meeting-room-1";
    /// 
    /// // With action ID for tracking
    /// var trackedList = new ConfbridgeListAction();
    /// trackedList.ActionId = "conf_001";
    /// </code>
    /// </remarks>
    /// <seealso cref="ConfbridgeListEvent"/>
    /// <seealso cref="ConfbridgeListCompleteEvent"/>
    /// <seealso cref="ConfbridgeKickAction"/>
    /// <seealso cref="ConfbridgeMuteAction"/>
    public class ConfbridgeListAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ConfbridgeList"</value>
        public override string Action => "ConfbridgeList";

        /// <summary>
        /// Gets or sets the conference identifier filter.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// The conference name to filter, or null for all conferences.
        /// </value>
        /// <remarks>
        /// Conference Filtering:
        /// 
        /// All Conferences (Conference = null):
        /// - Lists all active ConfBridge conferences
        /// - Returns comprehensive system-wide view
        /// - Useful for monitoring and dashboards
        /// - May return large number of events
        /// 
        /// Specific Conference (Conference specified):
        /// - Lists only the named conference
        /// - Reduces response size and processing
        /// - Useful for targeted monitoring
        /// - Returns single conference information
        /// 
        /// Conference Naming:
        /// 
        /// Common Naming Patterns:
        /// - Numeric: "1001", "2000", "9999"
        /// - Descriptive: "sales-meeting", "board-room"
        /// - Department: "support-conf", "dev-standup"
        /// - Scheduled: "meeting-20241201-0900"
        /// - Room-based: "conference-room-a"
        /// 
        /// Conference Identification:
        /// - Conference names are case-sensitive
        /// - Must match exactly as configured
        /// - Can be alphanumeric with special characters
        /// - Length limitations vary by configuration
        /// 
        /// Dynamic Conferences:
        /// - User-created conferences
        /// - Dynamically named based on extension
        /// - Generated from calendar systems
        /// - Ad-hoc meeting creation
        /// 
        /// Static Conferences:
        /// - Pre-configured in confbridge.conf
        /// - Persistent conference rooms
        /// - Dedicated meeting spaces
        /// - Department-specific conferences
        /// 
        /// Conference Discovery:
        /// - Use ConfbridgeListAction with no filter
        /// - Monitor conference creation events
        /// - Check ConfBridge configuration
        /// - Track user-initiated conferences
        /// 
        /// Filter Benefits:
        /// - Reduced network traffic
        /// - Faster response times
        /// - Targeted monitoring
        /// - Simplified event processing
        /// - Lower system overhead
        /// 
        /// Use Cases for Specific Filtering:
        /// - Conference-specific dashboards
        /// - Targeted participant monitoring
        /// - Individual conference management
        /// - Billing and usage tracking
        /// - Security monitoring
        /// 
        /// Use Cases for All Conferences:
        /// - System-wide monitoring
        /// - Capacity planning
        /// - Resource utilization
        /// - General administration
        /// - Comprehensive reporting
        /// 
        /// Configuration Examples:
        /// confbridge.conf:
        /// <code>
        /// [general]
        /// 
        /// [sales-meeting]
        /// type=bridge
        /// max_members=25
        /// record_conference=yes
        /// 
        /// [board-room]
        /// type=bridge
        /// max_members=10
        /// video_mode=follow_talker
        /// </code>
        /// 
        /// Error Handling:
        /// - Invalid conference name: Returns empty list
        /// - Non-existent conference: No error, empty response
        /// - Permission denied: Manager error response
        /// - System error: Action failure response
        /// </remarks>
        public string? Conference { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the ConfbridgeList action.
        /// </summary>
        /// <returns>The Type of ConfbridgeListCompleteEvent</returns>
        /// <remarks>
        /// Event Processing Flow:
        /// 
        /// 1. ConfbridgeListEvent (Multiple):
        ///    - One event per active conference
        ///    - Contains conference details and statistics
        ///    - Includes participant counts and status
        ///    - Provides conference configuration info
        /// 
        /// 2. ConfbridgeListCompleteEvent (Single):
        ///    - Marks end of conference list
        ///    - Indicates completion of action
        ///    - Contains final statistics
        ///    - Includes action correlation ID
        /// 
        /// Event Correlation:
        /// - All events include matching ActionID
        /// - Use ActionID to match events to this action
        /// - Process events until completion event received
        /// - Handle timeout if completion event not received
        /// 
        /// Processing Patterns:
        /// - Collect all ConfbridgeListEvents in a list
        /// - Process complete list when completion event received
        /// - Update real-time displays with individual events
        /// - Implement timeout handling for action completion
        /// 
        /// Example Event Handling:
        /// <code>
        /// var conferences = new List&lt;ConfbridgeListEvent&gt;();
        /// 
        /// // Handle individual conference events
        /// manager.ConfbridgeList += (sender, e) => {
        ///     if (e.ActionId == actionId) {
        ///         conferences.Add(e);
        ///         UpdateConferenceDisplay(e); // Real-time update
        ///     }
        /// };
        /// 
        /// // Handle completion
        /// manager.ConfbridgeListComplete += (sender, e) => {
        ///     if (e.ActionId == actionId) {
        ///         ProcessCompleteConferenceList(conferences);
        ///         CompleteAction();
        ///     }
        /// };
        /// </code>
        /// 
        /// Event Data Structure:
        /// ConfbridgeListEvent typical fields:
        /// - Conference: Conference name
        /// - Parties: Number of participants
        /// - Marked: Number of marked participants
        /// - Locked: Conference lock status
        /// - Muted: Number of muted participants
        /// - Activity: Conference activity level
        /// 
        /// Real-time Applications:
        /// - Conference monitoring dashboards
        /// - Participant count displays
        /// - Resource utilization meters
        /// - Billing and usage tracking
        /// - Security monitoring alerts
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(ConfbridgeListCompleteEvent);
        }
    }
}