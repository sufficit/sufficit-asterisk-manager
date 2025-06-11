using Sufficit.Asterisk.Manager.Response;
using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    ///     The GetVarAction queries for a channel variable.
    /// </summary>
    public class GetVarAction : ManagerActionResponse
    {
        /// <summary>
        ///     Creates a new GetVarAction that queries for the given global variable.
        /// </summary>
        /// <param name="variable">the name of the global variable to query.</param>
        public GetVarAction(string variable) : this(default!, variable) { }

        /// <summary>
        ///     Creates a new GetVarAction that queries for the given local channel variable.
        /// </summary>
        /// <param name="channel">the name of the channel, for example "SIP/1234-9cd".</param>
        /// <param name="variable">the name of the variable to query.</param>
        public GetVarAction (string channel, string variable)
        {
            Channel = channel;
            Variable = variable;
        }

        /// <summary>
        ///     Get the name of this action, i.e. "GetVar".
        /// </summary>
        public override string Action
        {
            get { return "GetVar"; }
        }

        /// <summary>
        ///     Get/Set the name of the channel, if you query for a local channel variable.
        ///     Leave empty to query for a global variable.
        /// </summary>
        public string Channel { get; set; } = default!;

        /// <summary>
        ///     Get/Set the name of the variable to query.
        /// </summary>
        public new string Variable { get; set; }

        public override Type ActionCompleteResponseClass()
            => typeof(GetVarResponse);
    }
}