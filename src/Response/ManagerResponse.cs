using System;
using System.Collections.Generic;
using Sufficit.Asterisk.Manager.Events.Abstracts;

namespace Sufficit.Asterisk.Manager.Response
{
    /// <summary>
    ///     Represents a response received from the Asterisk server as the result of a
    ///     previously sent ManagerAction.<br />
    ///     The response can be linked with the action that caused it by looking the
    ///     action id attribute that will match the action id of the corresponding
    ///     action.
    /// </summary>
    public class ManagerResponse : ManagerResponseEvent, IParseSupport
    {
        object IParseSupport.GetSetter() => this;

        void IParseSupport.Parse (string key, string value) => this.Parse(key, value);

        IDictionary<string, string> IParseSupport.ParseSpecial (IDictionary<string, string>? attributes)
            => attributes ?? new Dictionary<string, string>();
    }
}