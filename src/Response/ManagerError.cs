using System;

namespace Sufficit.Asterisk.Manager.Response
{
    /// <summary>
    ///     Represents an "Response: Error" response received from the asterisk server.
    ///     The cause for the error is given in the message attribute.
    /// </summary>
    public class ManagerError : ManagerResponse
    {
        public ManagerError()
        {
            this.Response = "Error";
        }

        public ManagerError(string message) : this()
        {
            this.Message = message;
        }

        public ManagerError(string message, Exception ex) : this(message)
        {
            this.Exception = ex;
        }

    }
}