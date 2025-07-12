using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// Deletes an entry from the Asterisk database for a given family and key.
    /// This action removes entries from the internal Asterisk database (AstDB).
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: DBDel
    /// Purpose: Remove an entry from the Asterisk database
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
    /// Response:
    /// - Success: Standard ManagerResponse indicating successful deletion
    /// - Error: ManagerError if the key doesn't exist or deletion fails
    /// 
    /// Delete Behavior:
    /// - If the key exists, it is permanently removed
    /// - If the key doesn't exist, an error response is returned
    /// - Deletion is immediate and cannot be undone
    /// - No confirmation is required or provided
    /// 
    /// Use Cases:
    /// - Removing outdated configuration
    /// - Clearing temporary settings
    /// - Disabling features by removing their database entries
    /// - Cleaning up obsolete registration information
    /// - Resetting user preferences to defaults
    /// - Removing blacklist entries
    /// 
    /// Security Considerations:
    /// - Deletion is permanent and immediate
    /// - No backup is created automatically
    /// - Consider backing up important data before deletion
    /// - Verify key existence before deletion if needed
    /// - Monitor for accidental deletions in logs
    /// 
    /// Related Actions:
    /// - Use DBGet to verify key exists before deletion
    /// - Use DBDelTree to delete entire family branches
    /// - Use DBPut to recreate entries if needed
    /// 
    /// CLI Equivalent:
    /// - "database del &lt;family&gt; &lt;key&gt;"
    /// 
    /// Example Usage:
    /// <code>
    /// // Remove call forward setting
    /// var dbDel = new DBDelAction("CFU", "1001");
    /// 
    /// // Delete device configuration
    /// var dbDel = new DBDelAction("DEVICE", "1001/dial");
    /// 
    /// // Remove blacklist entry
    /// var dbDel = new DBDelAction("BLACKLIST", "5551234567");
    /// </code>
    /// </remarks>
    /// <seealso cref="DBGetAction"/>
    /// <seealso cref="DBPutAction"/>
    /// <seealso cref="DBDelTreeAction"/>
    public class DBDelAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty DBDelAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set the Family and Key
        /// properties before sending the action.
        /// </remarks>
        public DBDelAction()
        {
        }

        /// <summary>
        /// Creates a new DBDelAction that deletes the database entry
        /// with the given key in the given family.
        /// </summary>
        /// <param name="family">The family (namespace) of the key (Required)</param>
        /// <param name="key">The key of the entry to delete (Required)</param>
        /// <remarks>
        /// Parameter Requirements:
        /// - Both parameters are required and cannot be null
        /// - Family and key must exactly match an existing entry
        /// - Case-sensitive exact match is performed
        /// - No wildcards or pattern matching supported
        /// 
        /// Deletion Verification:
        /// Consider using DBGet action first to verify the key exists
        /// if you need to distinguish between successful deletion and
        /// attempting to delete a non-existent key.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when family or key is null</exception>
        public DBDelAction(string family, string key)
        {
            Family = family ?? throw new ArgumentNullException(nameof(family));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// Creates a new DBDelAction with family, key, and action ID.
        /// </summary>
        /// <param name="family">The family (namespace) of the key (Required)</param>
        /// <param name="key">The key of the entry to delete (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of the response with this specific request.
        /// Useful for tracking the success/failure of database deletions.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when family or key is null</exception>
        public DBDelAction(string family, string key, string actionId) : this(family, key)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "DBDel"</value>
        public override string Action => "DBDel";

        /// <summary>
        /// Gets or sets the family of the entry to delete.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The database family name.
        /// </value>
        /// <remarks>
        /// Family Targeting:
        /// - Must exactly match the family of the entry to delete
        /// - Case-sensitive comparison performed
        /// - No pattern matching or wildcards supported
        /// 
        /// Common Deletion Scenarios by Family:
        /// 
        /// CFU (Call Forward Unconditional):
        /// - Delete to disable call forwarding for an extension
        /// - Example: Family="CFU", Key="1001"
        /// 
        /// DEVICE Configuration:
        /// - Remove device mappings when devices are decommissioned
        /// - Example: Family="DEVICE", Key="1001/dial"
        /// 
        /// BLACKLIST Management:
        /// - Remove numbers from blacklists when restrictions lifted
        /// - Example: Family="BLACKLIST", Key="5551234567"
        /// 
        /// User Preferences:
        /// - Reset user settings to system defaults
        /// - Example: Family="AMPUSER", Key="1001/language"
        /// 
        /// Temporary Data:
        /// - Clean up session or temporary configuration data
        /// - Example: Family="TEMP", Key="session_12345"
        /// </remarks>
        public string? Family { get; set; }

        /// <summary>
        /// Gets or sets the key of the entry to delete.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The database key name.
        /// </value>
        /// <remarks>
        /// Key Targeting:
        /// - Must exactly match the key of the entry to delete
        /// - Case-sensitive comparison performed
        /// - Hierarchical keys supported (with forward slashes)
        /// - No pattern matching or wildcards supported
        /// 
        /// Deletion Examples by Key Type:
        /// 
        /// Simple Keys:
        /// - "1001": Extension-specific setting
        /// - "enabled": Global feature toggle
        /// - "timeout": System timeout value
        /// 
        /// Hierarchical Keys:
        /// - "1001/dial": Device dial configuration
        /// - "1001/language": User language preference
        /// - "provider1/host": SIP provider host setting
        /// - "queue_sales/timeout": Queue-specific timeout
        /// 
        /// Feature-Specific Keys:
        /// - Call forwarding: Extension number (e.g., "1001")
        /// - Device mapping: Device identifier with attribute
        /// - User settings: User ID with setting name
        /// - Blacklist: Phone number to unblock
        /// 
        /// Key Validation:
        /// - Verify key format matches your application's convention
        /// - Ensure proper escaping of special characters if needed
        /// - Consider logging deletion operations for audit trails
        /// 
        /// Bulk Operations:
        /// For deleting multiple related keys, consider:
        /// - DBDelTree action for family/subtree deletion
        /// - Multiple individual DBDel actions for specific keys
        /// - Custom application logic for complex deletion patterns
        /// </remarks>
        public string? Key { get; set; }
    }
}