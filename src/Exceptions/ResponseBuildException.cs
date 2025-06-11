using System;
namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// An ResponseBuildException is thrown when a can't build the action response.
    /// </summary>
    public class ResponseBuildException : AsteriskManagerException
    {
		public ResponseBuildException(string message) : base(message) { }
		
		
		public ResponseBuildException(string message, Exception inner) : base(message, inner) { }
	}
}