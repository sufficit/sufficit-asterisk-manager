using System.Text.Json.Serialization;

namespace Sufficit.Asterisk.Manager.Response
{
    /// <summary>
    ///     Corresponds to a ChallengeAction and contains the challenge needed to log in using challenge/response.
    /// </summary>
    /// <seealso cref="Manager.Action.ChallengeAction" />
    /// <seealso cref="Manager.Action.LoginAction" />
    public class ChallengeResponse : ManagerResponse
    {
        /// <summary>
        ///     Get/Set the challenge to use when creating the key for log in.
        /// </summary>
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; } = default!;
    }
}