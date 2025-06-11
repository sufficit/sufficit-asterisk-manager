using AsterNET.Helpers;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Asterisk.Manager.Response;
using Sufficit.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    public static partial class ManagerResponseBuilder
    {
        private static ILogger _logger = ManagerLogger.CreateLogger(typeof(ManagerResponseBuilder));

        private static CultureInfo CultureInfo => Sufficit.Asterisk.Defaults.CultureInfo;

        public static ManagerResponseEvent BuildResponse (Type type, string actionId, IDictionary<string, string> buffer)
        {                        
            var response = (ManagerResponseEvent)Activator.CreateInstance(type)!;
            response.ActionId = StripInternalActionId(actionId);

            BindResponse(response, buffer);
            return response;
        }

        private static bool IsAsteriskSuccess(this ManagerResponseEvent? source)
        {
            if (source == null)
                return false;

            if (source.Exception != null)
                return false;

            if (string.IsNullOrWhiteSpace(source.Response))
                return false;

            var response = source.Response.ToLower().Trim();

            // Asterisk 1.4 and earlier
            if (response == "error" || response == "failure")
                return false;

            // Asterisk 1.6 and later
            // goodbye is a valid response for logoff
            //if (response == "success" || response == "goodbye")
            //    return true;

            // Unknown response, assume success
            return true;
        }

        internal static void BindResponse (ManagerResponseEvent response, IDictionary<string, string> buffer)
        {
            try
            {
                var attributes = response.BindFrom(buffer);
                response.Attributes = attributes;

                if (!response.IsAsteriskSuccess())
                    response.Exception = new AsteriskManagerActionException(response.Message);
            }
            catch (Exception ex)
            {
                response.Exception = new ResponseBuildException("unknown error on build response", ex);
                response.Attributes = new Dictionary<string, string>(buffer);                
            }
        }

        public static string StripInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return string.Empty;

            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex > 0)
            {
                if (actionId.Length > delimiterIndex + 1)
                    return actionId.Substring(delimiterIndex + 1).Trim();

                return actionId.Substring(0, delimiterIndex).Trim();
            }

            return string.Empty;
        }


        [Obsolete("not used anymore")]
        public static ManagerResponse BuildResponse (IDictionary<string, string> buffer)
        {
            ManagerResponse response;
            try
            {
                var responseType = DetermineResponseType(buffer);
                response = (ManagerResponse)Activator.CreateInstance(responseType)!;
                SetAttributes((IParseSupport)response, buffer);

                if (!response.IsAsteriskSuccess())                
                    response.Exception = new AsteriskManagerActionException(response.Message);                
            }
            catch (Exception ex)
            {
                response = new ManagerResponse
                {
                    Exception = new AsteriskManagerException("error on build response", ex),
                    Attributes = new Dictionary<string, string>(buffer)
                };
            }

            return response;
        }

        /// <summary>
        ///     Constructs an instance of ManagerResponse based on a map of attributes.
        /// </summary>
        /// <param name="attributes">the attributes and their values. The keys of this map must be all lower case.</param>
        /// <returns>the type of asterisk manager action response</returns>         
        [Obsolete("not used anymore")]
        public static Type DetermineResponseType(IDictionary<string, string> attributes)
        {
            if (attributes.ContainsKey("challenge"))
                return typeof(ChallengeResponse);
            
            if (attributes.ContainsKey("mailbox") && attributes.ContainsKey("waiting"))
                return typeof(MailboxStatusResponse);
            
            if (attributes.ContainsKey("mailbox") && attributes.ContainsKey("newmessages") && attributes.ContainsKey("oldmessages"))
                return typeof(MailboxCountResponse);
           
            if (attributes.ContainsKey("exten") && attributes.ContainsKey("context") && attributes.ContainsKey("hint") && attributes.ContainsKey("status"))
                return typeof(ExtensionStateResponse);
            
            if (attributes.ContainsKey("ping"))
                return typeof(PingResponse);

            if (attributes.ContainsKey("variable"))
                return typeof(GetVarResponse);

            return typeof(ManagerResponse);
        }

        /// <summary>
        /// Sets attributes on the specified object by parsing key-value pairs from the provided buffer. <br/>
        /// From <see cref="ManagerReader"/> safe buffer.
        /// </summary>
        /// <remarks>This method processes the attributes in the <paramref name="buffer"/> and attempts to
        /// set them on the object <paramref name="o"/>. Attributes are parsed using the <see
        /// cref="IParseSupport.ParseSpecial"/> method, and setters are resolved dynamically based on the object's type.
        /// If a setter is not found for an attribute, the attribute is passed to the <see cref="IParseSupport.Parse"/>
        /// method for custom handling.  Special attributes such as "event" and "userevent" are ignored. The "source"
        /// attribute is mapped to the "src" setter. If an attribute value cannot be converted to the expected type, it
        /// is skipped, and a warning is logged.  Exceptions during setter invocation are caught and logged, ensuring
        /// the method does not terminate prematurely.</remarks>
        /// <param name="o">The object implementing <see cref="IParseSupport"/> on which attributes will be set.</param>
        /// <param name="buffer">A dictionary containing attribute names and their corresponding values as strings.</param>
        public static void SetAttributes(IParseSupport o, IDictionary<string, string> buffer)
        {
            Type dataType;
            object? val;

            // Preparse attributes
            var attributes = o.ParseSpecial(buffer);

            var underlayingEvent = o.GetSetter();
            var underlayingType = underlayingEvent.GetType();
            var setters = GetSetters(underlayingType);

            foreach (var name in attributes.Keys)
            {
                if (name == "event" || name == "userevent") continue;

                MethodInfo? setter;

                if (name == "source")
                    setter = (MethodInfo?)setters["src"];
                else
                    setter = (MethodInfo?)setters[StripIllegalCharacters(name)];

                if (setter == null)
                {
                    o.Parse(name, attributes[name]);
                }
                else
                {
                    dataType = (setter.GetParameters()[0]).ParameterType;

                    if (!FromStringBuilderHelper.TryConvert(dataType, attributes[name], name, underlayingType, out val))
                    {
                        _logger.LogWarning("SetAttibutes keys: {json}", attributes.Keys.ToJsonOrError());
                        continue;
                    }

                    try
                    {
                        setter.Invoke(underlayingEvent, new[] { val });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "unable to set property '{name}' on {type}", name, underlayingType);
                    }
                }
            }
        }
        
        public static void SetAttributes (ManagerResponseEvent response, IDictionary<string, string> buffer)
        {
            Type dataType;
            object? val;

            var underlayingType = response.GetType();
            var setters = GetSetters(underlayingType);

            foreach (var name in buffer.Keys)
            {
                if (name == "event" || name == "userevent") continue;

                MethodInfo? setter;

                if (name == "source")
                    setter = (MethodInfo?)setters["src"];
                else
                    setter = (MethodInfo?)setters[StripIllegalCharacters(name)];

                if (setter == null)
                {
                    response.Parse(name, buffer[name]);
                }
                else
                {
                    dataType = (setter.GetParameters()[0]).ParameterType;

                    if (!FromStringBuilderHelper.TryConvert(dataType, buffer[name], name, underlayingType, out val))
                    {
                        _logger.LogWarning("SetAttibutes keys: {json}", buffer.Keys.ToJsonOrError());
                        continue;
                    }

                    try
                    {
                        setter.Invoke(underlayingType, new[] { val });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "unable to set property '{name}' on {type}", name, underlayingType);
                    }
                }
            }
        }
        
        /// <summary>
        ///     Returns a Map of setter methods of the given class.<br />
        ///     The key of the map contains the name of the attribute that can be accessed by the setter, the
        ///     value the setter itself. A method is considered a setter if its name starts with "set",
        ///     it is declared internal and takes no arguments.
        /// </summary>
        /// <param name="clazz">the class to return the setters for</param>
        /// <returns> a Map of attributes and their accessor methods (setters)</returns>
        internal static IDictionary GetSetters(Type clazz)
        {
            IDictionary accessors = new Hashtable();
            MethodInfo[] methods = clazz.GetMethods();
            string name;
            string methodName;
            MethodInfo method;

            for (int i = 0; i < methods.Length; i++)
            {
                method = methods[i];
                methodName = method.Name;
                // skip not "set..." methods and  skip methods with != 1 parameters
                if (!methodName.StartsWith("set_") || method.GetParameters().Length != 1)
                    continue;
                name = methodName.Substring("set_".Length).ToLower(CultureInfo);
                if (name.Length == 0) continue;
                accessors[name] = method;
            }
            return accessors;
        }

        /// <summary>
        ///     Strips all illegal charaters from the given lower case string.
        /// </summary>
        /// <param name="s">the original string</param>
        /// <returns>the string with all illegal characters stripped</returns>
        internal static string StripIllegalCharacters(string s)
        {
            char c;
            bool needsStrip = false;

            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));

            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c >= '0' && c <= '9')
                    continue;
                if (c >= 'a' && c <= 'z')
                    continue;
                if (c >= 'A' && c <= 'Z')
                    continue;
                needsStrip = true;
                break;
            }

            if (!needsStrip)
                return s;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c >= '0' && c <= '9')
                    sb.Append(c);
                else if (c >= 'a' && c <= 'z')
                    sb.Append(c);
                else if (c >= 'A' && c <= 'Z')
                    sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Extracts the value associated with the "actionid" key from the specified dictionary. <br />
        /// This buffer should implements StringComparer.OrdinalIgnoreCase
        /// </summary>
        /// <remarks>After extracting the "actionid" value, the key is removed from the dictionary. If the
        /// "actionid" key does not exist, this method will throw a <see cref="KeyNotFoundException"/>.</remarks>
        /// <param name="buffer">A dictionary containing key-value pairs, including the "actionid" key.</param>
        /// <returns>The value associated with the "actionid" key in the dictionary.</returns>
        internal static string ExtractActionId (IDictionary<string, string> buffer)
        {
            var actionId = buffer["actionid"];
            _ = buffer.Remove("actionid");
            return actionId;
        }

        internal static string GetInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex > 0)
                return actionId.Substring(0, delimiterIndex).Trim();
            return string.Empty;
        }
    }
}
