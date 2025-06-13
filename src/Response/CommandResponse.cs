using Sufficit.Asterisk.Manager.Events.Abstracts;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sufficit.Asterisk.Manager.Response
{
    /// <summary>
    ///     Corresponds to a CommandAction.<br />
    ///     Asterisk's handling of the command action is generelly quite hairy.
    ///     It sends a "Response: Follows" line followed by the raw output of the command including empty lines.
    ///     At the end of the command output a line containing "--END COMMAND--" is sent.
    ///     The reader parses this response into a CommandResponse object to hide these details.
    /// </summary>
    /// <seealso cref="Manager.Action.CommandAction" />
    public class CommandResponse : ManagerResponseEvent
    {
        /// <summary>
        ///     Get/Set a List containing strings representing the lines returned by the CLI command.
        /// </summary>
        /// <remarks>* for Multi line responses</remarks>
        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Result { get; set; }

        /// <summary>
        ///     Gets or sets the output value associated with the operation.
        /// </summary>
        /// <remarks>* for Single line responses</remarks>
        [JsonPropertyName("output")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull)]
        public string? Output { get; set; }
    }
}