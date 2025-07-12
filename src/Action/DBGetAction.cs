using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// Retrieves an entry in the Asterisk database for a given family and key.
    /// This action provides access to the internal Asterisk database (AstDB).
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: DBGet
    /// Purpose: Retrieve a value from the Asterisk database
    /// Privilege Required: system,call,all
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters:
    /// - Family: Database family (namespace) (Required)
    /// - Key: Database key (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Response Events:
    /// - DBGetResponseEvent: Contains the retrieved value
    /// - ManagerError: If the key doesn't exist or access is denied
    /// 
    /// Asterisk Database (AstDB):
    /// The Asterisk database is a simple key-value store that persists across
    /// Asterisk restarts. It's organized in a hierarchical structure using
    /// families (namespaces) and keys.
    /// 
    /// Database Structure:
    /// - Family: Top-level namespace (e.g., "SIP", "DEVICE", "CFB")
    /// - Key: Specific entry within the family
    /// - Value: Data associated with the key
    /// 
    /// Common Families:
    /// - "SIP": SIP peer registration information
    /// - "DEVICE": Device configuration mappings
    /// - "CFB": Call Forward Busy settings
    /// - "CFNR": Call Forward No Response settings
    /// - "CFU": Call Forward Unconditional settings
    /// - "CW": Call Waiting settings
    /// - "DND": Do Not Disturb settings
    /// - "AMPUSER": FreePBX user settings
    /// - "BLACKLIST": Blacklisted numbers
    /// 
    /// Usage Scenarios:
    /// - Reading configuration values
    /// - Checking feature settings
    /// - Retrieving persistent data
    /// - Accessing call forwarding settings
    /// - Reading user preferences
    /// - System status queries
    /// 
    /// Error Conditions:
    /// - Family or key not found
    /// - Invalid family/key format
    /// - Insufficient privileges
    /// - Database corruption
    /// 
    /// CLI Equivalent:
    /// - "database get &lt;family&gt; &lt;key&gt;"
    /// 
    /// Example Usage:
    /// <code>
    /// // Get SIP peer registration info
    /// var dbGet = new DBGetAction("SIP/Registry", "provider1");
    /// 
    /// // Get device mapping
    /// var dbGet = new DBGetAction("DEVICE", "1001/dial");
    /// 
    /// // Get call forward setting
    /// var dbGet = new DBGetAction("CFU", "1001");
    /// </code>
    /// </remarks>
    /// <seealso cref="DBGetResponseEvent"/>
    /// <seealso cref="DBPutAction"/>
    /// <seealso cref="DBDelAction"/>
    public class DBGetAction : ManagerActionEvent
    {
        /// <summary>
        /// Creates a new empty DBGetAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set the Family and Key
        /// properties before sending the action.
        /// </remarks>
        public DBGetAction()
        {
        }

        /// <summary>
        /// Creates a new DBGetAction that retrieves the value of the database entry
        /// with the given key in the given family.
        /// </summary>
        /// <param name="family">The family (namespace) of the key (Required)</param>
        /// <param name="key">The key of the entry to retrieve (Required)</param>
        /// <remarks>
        /// Family and Key Requirements:
        /// - Both parameters are required and cannot be null
        /// - Family acts as a namespace to organize related keys
        /// - Key is the specific identifier within the family
        /// - Case-sensitive exact match is performed
        /// 
        /// Family Examples:
        /// - "SIP": For SIP-related entries
        /// - "DEVICE": For device mappings
        /// - "CFU": For call forwarding settings
        /// - "AMPUSER": For user preferences
        /// 
        /// Key Format:
        /// - Can contain alphanumeric characters
        /// - Forward slashes often used for hierarchy: "1001/dial"
        /// - Underscores common for separation: "call_forward"
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when family or key is null</exception>
        public DBGetAction(string family, string key)
        {
            Family = family ?? throw new ArgumentNullException(nameof(family));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// Creates a new DBGetAction with family, key, and action ID.
        /// </summary>
        /// <param name="family">The family (namespace) of the key (Required)</param>
        /// <param name="key">The key of the entry to retrieve (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of the response with this specific request.
        /// Useful for tracking the success/failure of database retrievals.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when family or key is null</exception>
        public DBGetAction(string family, string key, string actionId) : this(family, key)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "DBGet"</value>
        public override string Action => "DBGet";

        /// <summary>
        /// Gets or sets the family (namespace) of the key.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The database family name.
        /// </value>
        /// <remarks>
        /// Family Guidelines:
        /// - Acts as a top-level namespace for organizing keys
        /// - Case-sensitive exact match required
        /// - Common families include system and application-specific namespaces
        /// 
        /// System Families:
        /// - "SIP": SIP channel driver data
        /// - "IAX": IAX2 channel driver data
        /// - "DAHDI": DAHDI channel driver data
        /// - "PJSIP": PJSIP channel driver data
        /// 
        /// Feature Families:
        /// - "CFU": Call Forward Unconditional
        /// - "CFB": Call Forward Busy
        /// - "CFNR": Call Forward No Response
        /// - "CW": Call Waiting
        /// - "DND": Do Not Disturb
        /// - "BLACKLIST": Call blocking lists
        /// 
        /// Application Families:
        /// - "DEVICE": Device configuration mapping
        /// - "AMPUSER": FreePBX user settings
        /// - "RINGTIMER": Ring timeout settings
        /// - "QUEUEMETRICS": Queue metrics data
        /// 
        /// Custom Families:
        /// Applications can create custom families for storing
        /// application-specific data in the Asterisk database.
        /// </remarks>
        public string? Family { get; set; }

        /// <summary>
        /// Gets or sets the key to retrieve.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The database key name.
        /// </value>
        /// <remarks>
        /// Key Structure:
        /// - Identifies specific entry within the family
        /// - Can use hierarchical naming with forward slashes
        /// - Case-sensitive exact match required
        /// 
        /// Key Examples by Family:
        /// 
        /// SIP Family:
        /// - "Registry/provider1": Registration info for provider1
        /// - "1001/host": Host setting for peer 1001
        /// - "1001/port": Port setting for peer 1001
        /// 
        /// DEVICE Family:
        /// - "1001/dial": Dial string for device 1001
        /// - "1001/type": Device type (fixed, adhoc, etc.)
        /// - "1001/user": Associated user for device 1001
        /// 
        /// CFU Family (Call Forward Unconditional):
        /// - "1001": Forward destination for extension 1001
        /// - "2000": Forward destination for extension 2000
        /// 
        /// AMPUSER Family (FreePBX):
        /// - "1001/device": Device association for user 1001
        /// - "1001/cidname": Caller ID name for user 1001
        /// - "1001/outboundcid": Outbound caller ID for user 1001
        /// 
        /// BLACKLIST Family:
        /// - "5551234567": Reason for blacklisting this number
        /// - "blocked/all": Global blocking status
        /// 
        /// Custom Keys:
        /// Applications can define their own key structures
        /// within custom families for application-specific needs.
        /// </remarks>
        public string? Key { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the DBGet action.
        /// </summary>
        /// <returns>The Type of DBGetResponseEvent</returns>
        /// <remarks>
        /// The DBGetResponseEvent contains:
        /// - Family: The requested family
        /// - Key: The requested key  
        /// - Val: The retrieved value (if found)
        /// - ActionID: Correlation identifier
        /// 
        /// If the key is not found, a ManagerError response is sent instead
        /// of a DBGetResponseEvent.
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(DBGetResponseEvent);
        }
    }
}