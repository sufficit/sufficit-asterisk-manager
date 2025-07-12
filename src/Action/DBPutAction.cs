using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// Adds or updates an entry in the Asterisk database for a given family, key, and value.
    /// This action provides write access to the internal Asterisk database (AstDB).
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: DBPut
    /// Purpose: Store a value in the Asterisk database
    /// Privilege Required: system,call,all
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters:
    /// - Family: Database family (namespace) (Required)
    /// - Key: Database key (Required)
    /// - Val: Value to store (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Response:
    /// - Success: Standard ManagerResponse indicating success
    /// - Error: ManagerError if the operation fails
    /// 
    /// Asterisk Database (AstDB):
    /// The Asterisk database is a simple key-value store that persists across
    /// Asterisk restarts. It's organized in a hierarchical structure using
    /// families (namespaces) and keys.
    /// 
    /// Database Persistence:
    /// - Data persists across Asterisk restarts
    /// - Stored in astdb.sqlite3 file (modern versions)
    /// - Atomic operations ensure data integrity
    /// - Can be backed up with standard database tools
    /// 
    /// Common Use Cases:
    /// - Storing user preferences and settings
    /// - Call forwarding configurations
    /// - Feature button assignments
    /// - Registration information caching
    /// - Application-specific configuration
    /// - Dynamic dialplan data
    /// 
    /// Value Limitations:
    /// - Values are stored as strings
    /// - Large values may impact performance
    /// - No complex data types (use JSON for structures)
    /// - Consider using external databases for large datasets
    /// 
    /// CLI Equivalent:
    /// - "database put &lt;family&gt; &lt;key&gt; &lt;value&gt;"
    /// 
    /// Example Usage:
    /// <code>
    /// // Set call forward destination
    /// var dbPut = new DBPutAction("CFU", "1001", "2000");
    /// 
    /// // Set device configuration
    /// var dbPut = new DBPutAction("DEVICE", "1001/dial", "SIP/1001");
    /// 
    /// // Store user preference
    /// var dbPut = new DBPutAction("AMPUSER", "1001/language", "en");
    /// </code>
    /// </remarks>
    /// <seealso cref="DBGetAction"/>
    /// <seealso cref="DBDelAction"/>
    public class DBPutAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty DBPutAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set the Family, Key, and Val
        /// properties before sending the action.
        /// </remarks>
        public DBPutAction()
        {
        }

        /// <summary>
        /// Creates a new DBPutAction that sets the value of the database entry 
        /// with the given key in the given family.
        /// </summary>
        /// <param name="family">The family (namespace) of the key (Required)</param>
        /// <param name="key">The key of the entry to set (Required)</param>
        /// <param name="val">The value to store (Required)</param>
        /// <remarks>
        /// Parameter Requirements:
        /// - All parameters are required and cannot be null
        /// - Family acts as a namespace to organize related keys
        /// - Key is the specific identifier within the family
        /// - Val is the data to be stored (converted to string)
        /// 
        /// Storage Considerations:
        /// - All values are stored as strings in the database
        /// - Empty strings are valid values
        /// - Null values will cause an error
        /// - Large values may impact database performance
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public DBPutAction(string family, string key, string val)
        {
            Family = family ?? throw new ArgumentNullException(nameof(family));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Val = val ?? throw new ArgumentNullException(nameof(val));
        }

        /// <summary>
        /// Creates a new DBPutAction with family, key, value, and action ID.
        /// </summary>
        /// <param name="family">The family (namespace) of the key (Required)</param>
        /// <param name="key">The key of the entry to set (Required)</param>
        /// <param name="val">The value to store (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of the response with this specific request.
        /// Useful for tracking the success/failure of database updates.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when family, key, or val is null</exception>
        public DBPutAction(string family, string key, string val, string actionId) : this(family, key, val)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "DBPut"</value>
        public override string Action => "DBPut";

        /// <summary>
        /// Gets or sets the family (namespace) of the key to set.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The database family name.
        /// </value>
        /// <remarks>
        /// Family Organization:
        /// - Serves as a namespace for organizing related keys
        /// - Case-sensitive exact match required
        /// - Should follow consistent naming conventions
        /// 
        /// Common System Families:
        /// - "SIP": SIP peer/registration data
        /// - "IAX": IAX2 peer data  
        /// - "DAHDI": DAHDI channel data
        /// - "PJSIP": PJSIP endpoint data
        /// 
        /// Feature Families:
        /// - "CFU": Call Forward Unconditional
        /// - "CFB": Call Forward Busy
        /// - "CFNR": Call Forward No Response
        /// - "CW": Call Waiting settings
        /// - "DND": Do Not Disturb settings
        /// - "BLACKLIST": Number blocking
        /// 
        /// Application Families:
        /// - "DEVICE": Device configuration
        /// - "AMPUSER": User preferences (FreePBX)
        /// - "CUSTOM": Application-specific data
        /// - "SETTINGS": System settings
        /// 
        /// Best Practices:
        /// - Use descriptive family names
        /// - Group related data in same family
        /// - Avoid overly generic names like "DATA"
        /// - Consider future expansion when naming
        /// </remarks>
        public string? Family { get; set; }

        /// <summary>
        /// Gets or sets the key to set.
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
        /// - Should be descriptive and consistent
        /// 
        /// Hierarchical Keys:
        /// Many applications use forward slashes to create
        /// hierarchical key structures within families:
        /// 
        /// Device Configuration:
        /// - "1001/dial": Dial string for device 1001
        /// - "1001/type": Device type (fixed, adhoc)
        /// - "1001/user": Associated user
        /// - "1001/defaultuser": Default user for device
        /// 
        /// User Settings:
        /// - "1001/language": User's preferred language
        /// - "1001/cidname": Caller ID name
        /// - "1001/outboundcid": Outbound caller ID
        /// - "1001/recording": Recording preference
        /// 
        /// Feature Settings:
        /// - "1001/enabled": Feature enabled status
        /// - "1001/timeout": Feature timeout value
        /// - "1001/destination": Feature destination
        /// 
        /// Key Naming Guidelines:
        /// - Use lowercase for consistency
        /// - Use underscores for word separation
        /// - Be descriptive but concise
        /// - Consider sortability for related keys
        /// - Avoid special characters except underscore and slash
        /// </remarks>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the value to store.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The value to be stored in the database.
        /// </value>
        /// <remarks>
        /// Value Characteristics:
        /// - All values are stored as strings
        /// - Empty strings are valid values
        /// - Unicode characters are supported
        /// - No specific length limit enforced by AMI
        /// 
        /// Data Types:
        /// While the database stores everything as strings,
        /// applications typically store:
        /// 
        /// Simple Values:
        /// - "enabled" / "disabled": Boolean-like values
        /// - "1001": Extension numbers
        /// - "30": Timeout values in seconds
        /// - "en_US": Language codes
        /// 
        /// Complex Values:
        /// - "SIP/1001&SIP/1002": Multiple destinations
        /// - "user:pass@host:port": Connection strings
        /// - "option1,option2,option3": Comma-separated lists
        /// - JSON strings: For complex data structures
        /// 
        /// Boolean Values:
        /// Common boolean representations:
        /// - "yes" / "no": Asterisk convention
        /// - "true" / "false": Programming convention
        /// - "1" / "0": Numeric boolean
        /// - "enabled" / "disabled": Feature states
        /// 
        /// Performance Considerations:
        /// - Very large values may impact database performance
        /// - Consider external storage for large data sets
        /// - Use efficient formats (JSON vs XML)
        /// - Avoid unnecessary whitespace in stored values
        /// 
        /// Special Values:
        /// - Empty string: Valid value, clears previous content
        /// - Whitespace: Preserved exactly as provided
        /// - Null: Not allowed, will cause error
        /// </remarks>
        public string? Val { get; set; }
    }
}