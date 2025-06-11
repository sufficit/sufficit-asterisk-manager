using System.Text.Json.Serialization;

namespace Sufficit.Asterisk.Manager.Response
{
    public class GetVarResponse : ManagerResponse
    {
        [JsonPropertyName("variable")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Variable { get; set; } = default!;

        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Value { get; set; } = string.Empty;    
    }
}