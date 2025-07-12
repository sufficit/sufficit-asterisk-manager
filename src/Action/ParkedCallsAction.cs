using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ParkedCallsAction requests a list of all currently parked calls.
    /// This action provides comprehensive information about calls in parking lots.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: ParkedCalls
    /// Purpose: Retrieve list of all parked calls
    /// Privilege Required: call,reporting,all
    /// Implementation: res/res_parking.c (modern) or res/res_features.c (legacy)
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters: None
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Response Flow:
    /// 1. Multiple ParkedCallEvent: One for each parked call
    /// 2. Single ParkedCallsCompleteEvent: Indicates end of list
    /// 
    /// Parked Call Information:
    /// Each ParkedCallEvent contains:
    /// - Channel: The parked channel name
    /// - From: Who parked the call
    /// - Timeout: When the call will timeout
    /// - ConnectedLineNum: Connected party number
    /// - ConnectedLineName: Connected party name
    /// - CallerIDNum: Caller ID number
    /// - CallerIDName: Caller ID name
    /// - ParkingSpace: The parking slot number
    /// - ParkingLot: The parking lot name
    /// 
    /// Call Parking Overview:
    /// - Calls placed in "parking lots" for retrieval
    /// - Each call assigned a parking space (extension)
    /// - Calls have configurable timeout periods
    /// - Multiple parking lots supported (modern Asterisk)
    /// 
    /// Usage Scenarios:
    /// - Call center supervision and monitoring
    /// - Reception desk call management
    /// - Real-time parking lot status displays
    /// - Call retrieval applications
    /// - Parking space management
    /// - Timeout monitoring and alerting
    /// - Call hold alternatives
    /// 
    /// Parking Lot Management:
    /// - Monitor parking space utilization
    /// - Track call parking patterns
    /// - Implement automatic call retrieval
    /// - Generate parking reports
    /// - Detect abandoned parked calls
    /// 
    /// Integration Applications:
    /// - Operator consoles
    /// - Attendant software
    /// - Call center dashboards
    /// - Mobile call management apps
    /// - Web-based phone systems
    /// - Hotel/hospitality systems
    /// 
    /// Real-time Monitoring:
    /// - Parking space availability
    /// - Call timeout warnings
    /// - Parking duration tracking
    /// - Usage statistics
    /// - Performance metrics
    /// 
    /// Asterisk Versions:
    /// - 1.2-1.8: Basic parking via res_features
    /// - 10+: Enhanced parking with named lots
    /// - 11+: Improved parking events and management
    /// - 12+: Complete parking framework rewrite (res_parking)
    /// - Modern: Advanced parking features and configuration
    /// 
    /// Parking Configuration:
    /// - Traditional: features.conf parkext, parkpos
    /// - Modern: res_parking.conf with named lots
    /// - Timeout settings per lot
    /// - Return contexts for timeout handling
    /// 
    /// Performance Considerations:
    /// - Lightweight action with minimal overhead
    /// - Number of events depends on active parked calls
    /// - Suitable for frequent monitoring
    /// - Consider event subscriptions for real-time updates
    /// 
    /// Implementation Notes:
    /// This action is implemented in parking modules (res_features or res_parking).
    /// Results reflect current parking state at time of request.
    /// Parking lots may have different configurations and policies.
    /// Modern Asterisk supports multiple named parking lots.
    /// 
    /// Error Conditions:
    /// - Parking not configured or loaded
    /// - Insufficient privileges
    /// - Parking module not available
    /// - Configuration errors
    /// 
    /// Example Usage:
    /// <code>
    /// // Get all parked calls
    /// var parkedCalls = new ParkedCallsAction();
    /// 
    /// // Get with action ID for tracking
    /// var parkedCalls = new ParkedCallsAction();
    /// parkedCalls.ActionId = "parked_001";
    /// 
    /// // Handle response events
    /// connection.SendActionAsync(parkedCalls);
    /// // Process ParkedCallEvent for each call
    /// // Process ParkedCallsCompleteEvent when done
    /// </code>
    /// </remarks>
    /// <seealso cref="ParkedCallEvent"/>
    /// <seealso cref="ParkedCallsCompleteEvent"/>
    /// <seealso cref="ParkAction"/>
    public class ParkedCallsAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ParkedCalls"</value>
        public override string Action => "ParkedCalls";

        /// <summary>
        /// Returns the event type that indicates completion of the ParkedCalls action.
        /// </summary>
        /// <returns>The Type of ParkedCallsCompleteEvent</returns>
        /// <remarks>
        /// Event Processing Flow:
        /// 
        /// 1. ParkedCallEvent (Multiple):
        ///    - One event per parked call
        ///    - Contains call details and parking information
        ///    - Includes caller information and timeout data
        ///    - Provides parking space and lot details
        /// 
        /// 2. ParkedCallsCompleteEvent (Single):
        ///    - Marks end of parked calls list
        ///    - Indicates completion of action
        ///    - Contains final count information
        ///    - Includes action correlation ID
        /// 
        /// Event Correlation:
        /// - All events include matching ActionID
        /// - Use ActionID to match events to this action
        /// - Process events until completion event received
        /// - Handle timeout if completion event not received
        /// 
        /// Processing Patterns:
        /// - Collect all ParkedCallEvents in a list
        /// - Process complete list when completion event received
        /// - Update real-time displays with individual events
        /// - Implement timeout handling for action completion
        /// 
        /// Example Event Handling:
        /// <code>
        /// var parkedCalls = new List&lt;ParkedCallEvent&gt;();
        /// 
        /// // Handle individual parked call events
        /// manager.ParkedCall += (sender, e) => {
        ///     if (e.ActionId == actionId) {
        ///         parkedCalls.Add(e);
        ///         UpdateDisplay(e); // Real-time update
        ///     }
        /// };
        /// 
        /// // Handle completion
        /// manager.ParkedCallsComplete += (sender, e) => {
        ///     if (e.ActionId == actionId) {
        ///         ProcessCompleteList(parkedCalls);
        ///         CompleteAction();
        ///     }
        /// };
        /// </code>
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(ParkedCallsCompleteEvent);
        }
    }
}