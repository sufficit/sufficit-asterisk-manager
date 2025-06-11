using AsterNET.Helpers;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Asterisk.Manager.Response;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sufficit.Asterisk.Manager
{
    public static class ManagerResponseExtensions
    {
        private static CultureInfo CultureInfo => Defaults.CultureInfo;

        /// <summary>
        ///     Throws an exception if the response is not successful.
        /// </summary>
        /// <param name="source"></param>
        public static void ThrowIfNotSuccess (this ManagerResponseEvent source, ManagerAction action)
        {
            if (source.Exception != null)
                throw new AsteriskManagerResponseFailedException("throw for not success", action, source.Exception);
        }


        public static bool IsSuccess (this ManagerResponseEvent? source)
        {
            if (source == null)
                return false;

            if (source.Exception != null)
                return false;

            return true;
        }

        /// <summary>
        ///     Unknown properties parser
        /// </summary>
        /// <param name="key">key name</param>
        /// <param name="value">key value</param>
        /// <returns>true - value parsed, false - can't parse value</returns>
        public static void Parse (this ManagerResponseEvent source, string key, string value)
        {
            if (source.Attributes == null)
                source.Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            source.Attributes.Parse(key, value);
        }


        [Obsolete("not used anymore")]
        public static string ToString(this ManagerResponse source)
            => Helper.ToString(source);

        /// <summary>
        ///     Returns the value of the attribute with the given key.<br />
        ///     This is particulary important when a response contains special
        ///     attributes that are dependent on the action that has been sent.<br />
        ///     An example of this is the response to the GetVarAction.
        ///     It contains the value of the channel variable as an attribute
        ///     stored under the key of the variable name.<br />
        ///     Example:
        ///     <pre>
        ///         GetVarAction action = new GetVarAction();
        ///         action.setChannel("SIP/1310-22c3");
        ///         action.setVariable("ALERT_INFO");
        ///         ManagerResponse response = connection.SendAction(action);
        ///         String alertInfo = response.getAttribute("ALERT_INFO");
        ///     </pre>
        ///     As all attributes are internally stored in lower case the key is
        ///     automatically converted to lower case before lookup.
        /// </summary>
        /// <param name="key">the key to lookup.</param>
        /// <returns>
        ///     the value of the attribute stored under this key or
        ///     null if there is no such attribute.
        /// </returns>
        [Obsolete("not used anymore")]
        public static string GetAttribute(this ManagerResponse source, string key)
        {
            if (source.Attributes == null)
                return string.Empty;

            var normalized = key.ToLower(CultureInfo);
            if (!source.Attributes.ContainsKey(normalized))
                return string.Empty;

            return source.Attributes[normalized];
        }

        /// <summary>
        /// Returns an enumerator that iterates through all lines of a CommandResponse.
        /// It yields the 'Output' property first (if it exists), 
        /// followed by all lines from the 'Result' list.
        /// </summary>
        /// <param name="response">The command response instance.</param>
        /// <returns>An IEnumerable<string> containing all lines from the response.</returns>
        public static IEnumerable<string> GetResponse (this CommandResponse response)
        {
            // 1. Retorna a primeira linha (Output) se ela existir
            if (!string.IsNullOrEmpty(response.Output))
            {
                yield return response.Output;
            }

            // 2. Retorna as outras linhas (Result) se a lista existir
            if (response.Result != null)
            {
                foreach (string line in response.Result)
                {
                    yield return line;
                }
            }
        }
    }
}
