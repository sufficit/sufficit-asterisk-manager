using AsterNET.Manager.Action;
using AsterNET.Manager.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Sufficit.Asterisk.Manager
{
    public class ManagerResponseException : Exception
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("response")]
        public ManagerResponse? Response { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("action")]
        public ManagerAction? Action { get; set; }

        public ManagerResponseException (ManagerResponse? source, ManagerAction? action = null) 
        { 
            Response = source; 
            Action = action; 
        }

        public string GetMessage()
        {
            string message = "manager response exception";
            if (Action != null)
                message += $", action: {Action.Action}, id: {Action.ActionId}";

            message += $", message: {Response?.Message ?? "unexpected null"}";
            return message;
        }

        public override string Message => GetMessage();
    }   
}
