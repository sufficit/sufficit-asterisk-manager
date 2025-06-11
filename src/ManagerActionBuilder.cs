using AsterNET.Helpers;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Action;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    public static partial class ManagerActionBuilder
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger(typeof(ManagerActionBuilder));
        private static CultureInfo CultureInfo => Defaults.CultureInfo;

        private static readonly string[] IgnoreKeys = { "class", "action", "actionid", "dictionary" };
        
        /// <summary>
        /// Builds the action string without a custom variable delimiter.
        /// </summary>
        public static string BuildAction (ManagerAction action, string internalActionId)
        {
            // O delimitador padrão é vírgula.
            return BuildAction(action, internalActionId, new char[] { ',' });
        }

        public static string BuildAction(ManagerAction action, string? internalActionId, char[] varDelimiter)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            MethodInfo getter;
            object? value;
            var sb = new StringBuilder();
            string? valueAsString;

            // Use Sufficit.Asterisk.Manager.Action.ProxyAction
            if (action is ProxyAction proxyAction)
                sb.Append(string.Concat("ProxyAction: ", proxyAction.Action, Common.LINE_SEPARATOR)); // Assuming Common.LINE_SEPARATOR is from Sufficit.Asterisk.Manager.Helpers
            else
                sb.Append(string.Concat("Action: ", action.Action, Common.LINE_SEPARATOR));

            string amiActionIdHeader = action.ActionId;
            if (!string.IsNullOrEmpty(internalActionId))
            {
                amiActionIdHeader = string.IsNullOrEmpty(action.ActionId) ?
                    internalActionId :
                    string.Concat(internalActionId, Common.INTERNAL_ACTION_ID_DELIMITER, action.ActionId); // Assuming Common constants
            }
            if (!string.IsNullOrEmpty(amiActionIdHeader))
                sb.Append(string.Concat("ActionID: ", amiActionIdHeader, Common.LINE_SEPARATOR));

            if (action.Dictionary != null) // ManagerAction.Dictionary is IDictionary?
            {
                foreach (DictionaryEntry entry in action.Dictionary)
                {
                    if (entry.Key != null && entry.Value != null)
                    {
                        sb.Append(string.Concat(entry.Key.ToString(), ": ", entry.Value.ToString(), Common.LINE_SEPARATOR));
                    }
                }
            }

            // Assuming Helper.GetGetters is from Sufficit.Asterisk.Manager.Helpers.Helper
            var type = action.GetType();
            _logger.LogDebug("building action for type: {type}", type.FullName);
            
            var getters = Helper.GetGetters(type);
            foreach (string nameKey in getters.Keys)
            {
                string nameLower = nameKey.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                if (IgnoreKeys.Contains(nameLower))
                    continue;

                getter = getters[nameKey];
                Type propType = getter.ReturnType;

                if (!(propType == typeof(string)
                    || propType == typeof(bool)
                    || propType == typeof(double)
                    || propType == typeof(DateTime)
                    || propType == typeof(int)
                    || propType == typeof(long)
                    || propType == typeof(Dictionary<string, string>)
                    || typeof(IDictionary).IsAssignableFrom(propType)
                    ))
                {
                    continue;
                }

                try
                {
                    value = getter.Invoke(action, Array.Empty<object>());
                }
                catch (UnauthorizedAccessException uae) { throw new AsteriskManagerException($"Unable to retrieve property '{nameKey}' from {action.GetType().FullName}", uae); }
                catch (TargetInvocationException tie) { throw new AsteriskManagerException($"Error retrieving property '{nameKey}' from {action.GetType().FullName}", tie.InnerException ?? tie); }

                if (value == null)
                    continue;

                if (value is string strValue)
                {
                    if (strValue.Length == 0) continue;
                    valueAsString = strValue;
                }
                else if (value is bool boolValue)
                {
                    valueAsString = boolValue ? "true" : "false";
                }
                else if (value is DateTime dateTimeValue)
                {
                    valueAsString = dateTimeValue.ToString(CultureInfo);
                }
                else if (value is IDictionary dictionaryValue)
                {
                    // Assuming Helper.JoinVariables is from Sufficit.Asterisk.Manager.Helpers.Helper
                    valueAsString = JoinVariables(dictionaryValue, Common.LINE_SEPARATOR, ": ");
                    if (valueAsString.Length == 0)
                        continue;
                    sb.Append(valueAsString);
                    sb.Append(Common.LINE_SEPARATOR);
                    continue;
                }
                else
                {
                    valueAsString = Convert.ToString(value, CultureInfo);
                }

                if (!string.IsNullOrEmpty(valueAsString))
                    sb.Append(string.Concat(nameKey, ": ", valueAsString, Common.LINE_SEPARATOR));
            }

            if (action.Variable != null && action.Variable.Count > 0) // ManagerAction.Variable is NameValueCollection?
            {
                // Assuming Helper.JoinVariables is from Sufficit.Asterisk.Manager.Helpers.Helper
                string nvcValues = JoinVariables(action.Variable, new string(varDelimiter), "=");
                if (!string.IsNullOrEmpty(nvcValues))
                {
                    sb.Append(string.Concat("Variable: ", nvcValues, Common.LINE_SEPARATOR));
                }
            }

            sb.Append(Common.LINE_SEPARATOR);
            return sb.ToString();
        }

        public static string JoinVariables(NameValueCollection? collection, string delim, string delimKeyValue)
        {
            if (collection == null || collection.Count == 0)
            {
                return string.Empty;
            }

            // Transform NameValueCollection to IEnumerable<KeyValuePair<object?, object?>>
            // NameValueCollection can have multiple values for the same key, and values can be null.
            // GetValues(key) returns null if the key has no associated values (e.g., "key&" in a query string).
            var items = collection.AllKeys.SelectMany(
                key => collection.GetValues(key) ?? Enumerable.Empty<string?>(), // Ensures that a null result from GetValues doesn't break SelectMany
                (keyObj, value) => new KeyValuePair<object?, object?>(keyObj, value) // keyObj from AllKeys is string, value is string?
            );

            return CoreJoinLogic(items, delim, delimKeyValue);
        }

        public static string JoinVariables(IDictionary? dictionary, string delim, string delimKeyValue)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return string.Empty;
            }

            // Transform IDictionary to IEnumerable<KeyValuePair<object?, object?>>
            // DictionaryEntry.Key and .Value are of type object.
            var items = dictionary.Cast<DictionaryEntry>()
                                  .Select(de => new KeyValuePair<object?, object?>(de.Key, de.Value));

            return CoreJoinLogic(items, delim, delimKeyValue);
        }

        /// <summary>
        ///     Private core method containing the main joining logic.
        /// </summary>
        public static string CoreJoinLogic(IEnumerable<KeyValuePair<object?, object?>> keyValuePairs, string itemDelimiter, string pairDelimiter)
        {
            var sb = new StringBuilder();
            foreach (var pair in keyValuePairs)
            {
                if (sb.Length > 0)
                    sb.Append(itemDelimiter);

                sb.Append(pair.Key);
                sb.Append(pairDelimiter);
                sb.Append(pair.Value);
            }
            return sb.ToString();
        }
    }
}
