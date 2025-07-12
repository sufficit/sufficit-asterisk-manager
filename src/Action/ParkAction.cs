using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ParkAction parks a channel in a call parking lot.
    /// This action enables call hold functionality with automatic timeout and retrieval features.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Park
    /// Purpose: Park a call in a parking lot for later retrieval
    /// Privilege Required: call,all
    /// Implementation: res/res_parking.c (modern) or res/res_features.c (legacy)
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters:
    /// - Channel: The channel to park (Required)
    /// - Channel2: Timeout destination channel (Required)
    /// 
    /// Optional Parameters:
    /// - Timeout: Parking timeout in milliseconds (Optional)
    /// - Parkinglot: Specific parking lot name (Optional)
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Parking Behavior:
    /// - Channel is moved to parking lot
    /// - Assigned parking space number
    /// - Caller hears parking music or silence
    /// - Can be retrieved by dialing parking space
    /// - Automatically returns if timeout reached
    /// 
    /// Timeout Handling:
    /// - If timeout reached, call returns to Channel2
    /// - Channel2 receives the returning call
    /// - Prevents abandoned parked calls
    /// - Configurable timeout values
    /// 
    /// Usage Scenarios:
    /// - Reception desk call holding
    /// - Operator console parking
    /// - Call center supervision
    /// - Multi-line phone systems
    /// - Conference call management
    /// - Call retrieval systems
    /// - Customer service workflows
    /// - PBX attendant features
    /// 
    /// Reception Applications:
    /// - Hold calls while finding recipients
    /// - Queue management
    /// - Call screening processes
    /// - Multiple call handling
    /// - Visitor notification systems
    /// 
    /// Call Center Applications:
    /// - Supervisor parking
    /// - Agent call transfers
    /// - Quality monitoring setup
    /// - Training scenarios
    /// - Emergency call handling
    /// 
    /// Parking Lots:
    /// - Default lot: System default parking
    /// - Named lots: Specific department lots
    /// - Multiple lots: Different timeout/return rules
    /// - Lot capacity: Maximum parked calls
    /// 
    /// Parking Process:
    /// 1. Validate channels exist and are active
    /// 2. Find available parking space
    /// 3. Move channel to parking lot
    /// 4. Assign parking space number
    /// 5. Start timeout timer
    /// 6. Generate parking events
    /// 
    /// Retrieval Methods:
    /// - Dial parking space number
    /// - Manager action retrieval
    /// - Timeout return to Channel2
    /// - Administrative retrieval
    /// 
    /// Asterisk Versions:
    /// - 1.2-1.8: Basic parking via res_features
    /// - 10+: Enhanced parking with named lots
    /// - 11+: Improved parking events
    /// - 12+: Complete parking rewrite (res_parking)
    /// - Modern: Advanced parking features
    /// 
    /// Implementation Notes:
    /// This action uses Asterisk's parking subsystem.
    /// Parking configuration affects available features.
    /// Events are generated during parking operations.
    /// Timeout values should be reasonable for use case.
    /// 
    /// Configuration Requirements:
    /// - Parking module must be loaded
    /// - Parking lots must be configured
    /// - Extensions for retrieval must exist
    /// - Music on hold may be configured
    /// 
    /// Error Conditions:
    /// - Channel not found or not active
    /// - Channel2 invalid for timeout returns
    /// - Parking lot full or unavailable
    /// - Parking not configured
    /// - Insufficient privileges
    /// 
    /// Performance Considerations:
    /// - Minimal overhead for parking operation
    /// - Memory usage for parked call tracking
    /// - Timer resources for timeout handling
    /// - Event generation overhead
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Parking lot access control
    /// - Timeout destination validation
    /// - Parking space number exposure
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic parking with timeout
    /// var park = new ParkAction(
    ///     "SIP/1001-00000001", 
    ///     "SIP/operator-00000002", 
    ///     "60000"  // 60 second timeout
    /// );
    /// 
    /// // Parking in specific lot
    /// var parkSpecific = new ParkAction(
    ///     "SIP/1001-00000001", 
    ///     "SIP/operator-00000002", 
    ///     "120000", // 2 minute timeout
    ///     "reception-lot"
    /// );
    /// </code>
    /// </remarks>
    /// <seealso cref="ParkedCallsAction"/>
    public class ParkAction : ManagerAction
    {
        /// <summary>
        /// Creates a new ParkAction with basic parameters.
        /// </summary>
        /// <param name="channel">The channel to park (Required)</param>
        /// <param name="channel2">Timeout destination channel (Required)</param>
        /// <param name="timeout">Timeout in milliseconds (Required)</param>
        /// <remarks>
        /// Basic Parking:
        /// - Uses default parking lot
        /// - Standard timeout behavior
        /// - Simple park and retrieve model
        /// 
        /// Parameter Requirements:
        /// - Channel must be active and connected
        /// - Channel2 must be valid for timeout returns
        /// - Timeout should be reasonable (30s - 10min typical)
        /// 
        /// Default Lot Usage:
        /// - Uses system default parking lot
        /// - Standard parking space assignment
        /// - Default music on hold
        /// - Standard retrieval extensions
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any parameter is empty</exception>
        public ParkAction(string channel, string channel2, string timeout)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be empty", nameof(channel));
            if (channel2 == null)
                throw new ArgumentNullException(nameof(channel2));
            if (string.IsNullOrWhiteSpace(channel2))
                throw new ArgumentException("Channel2 cannot be empty", nameof(channel2));
            if (timeout == null)
                throw new ArgumentNullException(nameof(timeout));
            if (string.IsNullOrWhiteSpace(timeout))
                throw new ArgumentException("Timeout cannot be empty", nameof(timeout));

            Channel = channel;
            Channel2 = channel2;
            Timeout = timeout;
        }

        /// <summary>
        /// Creates a new ParkAction with specific parking lot.
        /// </summary>
        /// <param name="channel">The channel to park (Required)</param>
        /// <param name="channel2">Timeout destination channel (Required)</param>
        /// <param name="timeout">Timeout in milliseconds (Required)</param>
        /// <param name="parkinglot">Specific parking lot name (Required)</param>
        /// <remarks>
        /// Named Lot Parking:
        /// - Uses specific named parking lot
        /// - Lot-specific configuration applies
        /// - Dedicated parking space ranges
        /// - Custom timeout and return behavior
        /// 
        /// Parking Lot Benefits:
        /// - Department separation
        /// - Different timeout policies
        /// - Specialized music on hold
        /// - Custom retrieval procedures
        /// - Access control per lot
        /// 
        /// Common Lot Names:
        /// - "reception": Reception desk parking
        /// - "sales": Sales department parking
        /// - "support": Support team parking
        /// - "management": Management parking
        /// - "emergency": Emergency parking
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any parameter is empty</exception>
        public ParkAction(string channel, string channel2, string timeout, string parkinglot) : this(channel, channel2, timeout)
        {
            if (parkinglot == null)
                throw new ArgumentNullException(nameof(parkinglot));
            if (string.IsNullOrWhiteSpace(parkinglot))
                throw new ArgumentException("Parkinglot cannot be empty", nameof(parkinglot));

            Parkinglot = parkinglot;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Park"</value>
        public override string Action => "Park";

        /// <summary>
        /// Gets or sets the channel to park.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name to park.
        /// </value>
        /// <remarks>
        /// Channel Requirements:
        /// 
        /// Channel State Requirements:
        /// - Must be an active, connected channel
        /// - Should be in "Up" state (answered)
        /// - Must be accessible for parking operations
        /// - Should not already be in a parking lot
        /// 
        /// Channel Format Examples:
        /// - "SIP/1001-00000001" (SIP channel)
        /// - "PJSIP/1001-00000001" (PJSIP channel)
        /// - "IAX2/provider-00000001" (IAX2 channel)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// 
        /// Parking Eligibility:
        /// - Channel must support parking operations
        /// - Should have audio path established
        /// - Must not be in certain special states
        /// - Channel technology must allow parking
        /// 
        /// Common Parking Sources:
        /// - Reception desk calls
        /// - Operator console channels
        /// - Queue agent channels
        /// - Direct dial calls
        /// - Transfer target channels
        /// 
        /// Channel Discovery:
        /// - Use CoreShowChannelsAction for active channels
        /// - Monitor bridge events for connected channels
        /// - Track channels from originate actions
        /// - Verify channel state before parking
        /// 
        /// Pre-parking Considerations:
        /// - Verify channel is not already parked
        /// - Check channel supports parking features
        /// - Ensure channel has established audio
        /// - Validate channel is in correct state
        /// 
        /// Error Scenarios:
        /// - Channel not found: Channel may have hung up
        /// - Channel busy: Channel in non-parkable state
        /// - Permission denied: Insufficient parking privileges
        /// - Already parked: Channel already in parking lot
        /// </remarks>
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the channel where the call returns after timeout.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The destination channel for timeout returns.
        /// </value>
        /// <remarks>
        /// Timeout Channel Purpose:
        /// 
        /// Return Destination:
        /// - Receives call when parking timeout expires
        /// - Should be monitored for returning calls
        /// - Typically the originating operator/reception
        /// - Must be active when timeout occurs
        /// 
        /// Channel2 Requirements:
        /// - Should be an active, monitored channel
        /// - Must be able to receive calls
        /// - Should have appropriate call handling
        /// - Typically operator or reception channel
        /// 
        /// Common Return Channels:
        /// - "SIP/operator-12345": Operator console
        /// - "SIP/reception-67890": Reception desk
        /// - "Queue/callback-queue": Callback queue
        /// - "Local/timeout@parking-context": Timeout handler
        /// 
        /// Return Scenarios:
        /// 
        /// Operator Return:
        /// - Call returns to original operator
        /// - Operator handles unretrieved call
        /// - May offer different options
        /// - Can re-park or handle directly
        /// 
        /// Queue Return:
        /// - Call enters queue for handling
        /// - Distributed to available agents
        /// - Maintains service level
        /// - Automatic call distribution
        /// 
        /// Voicemail Return:
        /// - Call redirected to voicemail
        /// - Caller can leave message
        /// - Prevents call abandonment
        /// - Automated message handling
        /// 
        /// Timeout Handling Best Practices:
        /// - Monitor return channels for activity
        /// - Implement appropriate timeout handling
        /// - Consider caller experience during timeout
        /// - Provide alternative options if possible
        /// - Log timeout events for analysis
        /// 
        /// Return Channel Validation:
        /// - Verify channel exists when parking
        /// - Ensure channel is monitored
        /// - Check channel can handle returns
        /// - Test timeout scenarios regularly
        /// 
        /// Channel2 Configuration:
        /// - Should be persistent/stable channel
        /// - May need special dialplan handling
        /// - Consider capacity for multiple returns
        /// - Implement proper error handling
        /// </remarks>
        public string? Channel2 { get; set; }

        /// <summary>
        /// Gets or sets the parking timeout in milliseconds.
        /// This property is required.
        /// </summary>
        /// <value>
        /// Timeout value in milliseconds.
        /// </value>
        /// <remarks>
        /// Timeout Configuration:
        /// 
        /// Timeout Purpose:
        /// - Prevents abandoned parked calls
        /// - Returns calls to monitored location
        /// - Ensures caller doesn't wait indefinitely
        /// - Maintains service quality
        /// 
        /// Timeout Value Guidelines:
        /// 
        /// Common Timeout Values:
        /// - "30000": 30 seconds (very short)
        /// - "60000": 1 minute (quick retrieval)
        /// - "120000": 2 minutes (standard)
        /// - "300000": 5 minutes (extended)
        /// - "600000": 10 minutes (maximum recommended)
        /// 
        /// Business Context Timeouts:
        /// 
        /// Reception Desk:
        /// - 60-120 seconds: Quick location of recipients
        /// - Allows time to find person or take message
        /// - Prevents caller frustration
        /// 
        /// Call Center:
        /// - 30-60 seconds: Supervisor consultations
        /// - Quick decisions or escalations
        /// - Maintains call flow
        /// 
        /// Medical/Emergency:
        /// - 30-60 seconds: Critical communication
        /// - Rapid response requirements
        /// - Patient safety considerations
        /// 
        /// Customer Service:
        /// - 120-300 seconds: Research or consultation
        /// - Complex issue resolution
        /// - Supervisor approval processes
        /// 
        /// Timeout Considerations:
        /// 
        /// Caller Experience:
        /// - Too short: Premature returns, poor experience
        /// - Too long: Caller frustration, abandonment
        /// - Just right: Efficient service, good experience
        /// 
        /// Staff Workflow:
        /// - Allow sufficient time for task completion
        /// - Consider complexity of typical requests
        /// - Account for staff availability patterns
        /// - Balance efficiency with service quality
        /// 
        /// System Resources:
        /// - Longer timeouts use more memory
        /// - More parked calls require more tracking
        /// - Timer resources for timeout management
        /// - Event processing overhead
        /// 
        /// Timeout Best Practices:
        /// - Test timeout values with real usage
        /// - Monitor timeout frequency and patterns
        /// - Adjust based on business requirements
        /// - Consider time-of-day variations
        /// - Document timeout policies
        /// 
        /// Special Timeout Values:
        /// - "0": Infinite timeout (not recommended)
        /// - Very large values: Effectively infinite
        /// - Negative values: May cause errors
        /// - Non-numeric: Will cause action failure
        /// </remarks>
        public string? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the parking lot name.
        /// This property is optional (uses default lot if not specified).
        /// </summary>
        /// <value>
        /// The parking lot name, or null for default lot.
        /// </value>
        /// <remarks>
        /// Parking Lot Configuration:
        /// 
        /// Default vs Named Lots:
        /// - Default: System default parking lot
        /// - Named: Specific configured parking lots
        /// - Multiple lots: Different policies per lot
        /// - Lot separation: Department or function based
        /// 
        /// Named Lot Benefits:
        /// 
        /// Department Separation:
        /// - "sales-lot": Sales team parking
        /// - "support-lot": Support team parking
        /// - "management-lot": Management parking
        /// - "reception-lot": Reception desk parking
        /// 
        /// Different Policies:
        /// - Timeout values per lot
        /// - Music on hold per lot
        /// - Space number ranges per lot
        /// - Access control per lot
        /// - Return destinations per lot
        /// 
        /// Common Lot Names:
        /// 
        /// Functional Lots:
        /// - "reception": Reception/front desk
        /// - "operator": Operator console
        /// - "emergency": Emergency services
        /// - "callback": Callback parking
        /// - "conference": Conference preparation
        /// 
        /// Department Lots:
        /// - "sales": Sales department
        /// - "support": Customer support
        /// - "billing": Billing department
        /// - "technical": Technical support
        /// - "management": Management team
        /// 
        /// Location Lots:
        /// - "building-a": Geographic separation
        /// - "floor-2": Floor-based lots
        /// - "branch-main": Branch office lots
        /// - "remote": Remote worker lots
        /// 
        /// Parking Lot Configuration:
        /// Modern Asterisk (res_parking.conf):
        /// <code>
        /// [reception-lot]
        /// type=parking_lot
        /// parkext=700
        /// parkpos=701-720
        /// context=parked-calls
        /// parkingtime=120
        /// comebacktoorigin=yes
        /// 
        /// [sales-lot]
        /// type=parking_lot
        /// parkext=800
        /// parkpos=801-820
        /// context=parked-calls
        /// parkingtime=300
        /// comebacktoorigin=no
        /// comebackcontext=sales-callback
        /// </code>
        /// 
        /// Lot Selection Strategy:
        /// - Use named lots for organized environments
        /// - Default lot for simple setups
        /// - Department lots for large organizations
        /// - Function lots for specialized needs
        /// 
        /// Lot Capacity Planning:
        /// - Estimate concurrent parked calls
        /// - Plan space number ranges
        /// - Consider peak usage periods
        /// - Monitor lot utilization
        /// 
        /// Error Handling:
        /// - Invalid lot name: Action may fail
        /// - Lot full: No available spaces
        /// - Lot not configured: System error
        /// - Access denied: Permission issues
        /// </remarks>
        public string? Parkinglot { get; set; }
    }
}