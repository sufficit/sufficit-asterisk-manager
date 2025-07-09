using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     Retrieves an entry in the Asterisk database for a given family and key.
    ///     If an entry is found a DBGetResponseEvent is sent by Asterisk containing the
    ///     value, otherwise a ManagerError indicates that no entry matches.
    ///     Available since Asterisk 1.2
    /// </summary>
    /// <seealso cref="DBGetResponseEvent"/>
    public class DBGetAction : ManagerActionEvent
    {
        /// <summary>
        ///     Creates a new empty DBGetAction.
        /// </summary>
        public DBGetAction()
        {
        }

        /// <summary>
        ///     Creates a new DBGetAction that retrieves the value of the database entry
        ///     with the given key in the given family.
        /// </summary>
        /// <param name="family">the family of the key</param>
        /// <param name="key">the key of the entry to retrieve</param>
        public DBGetAction(string family, string key)
        {
            Family = family;
            Key = key;
        }

        /// <summary>
        ///     Gets the name of this action.
        /// </summary>
        /// <value>Always returns "DBGet"</value>
        public override string Action => "DBGet";

        /// <summary>
        ///     Gets or sets the family (namespace) of the key.
        /// </summary>
        public string? Family { get; set; }

        /// <summary>
        ///     Gets or sets the key to retrieve.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        ///     Returns the event type that indicates completion of the DBGet action.
        /// </summary>
        /// <returns>The Type of DBGetResponseEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(DBGetResponseEvent);
        }
    }
}