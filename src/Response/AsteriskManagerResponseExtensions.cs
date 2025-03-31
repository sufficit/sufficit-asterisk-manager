using AsterNET.Manager.Action;
using AsterNET.Manager.Response;
using System;
using System.Collections.Generic;
using System.Resources;
using System.Text;

namespace Sufficit.Asterisk.Manager.Response
{
    public static class AsteriskManagerResponseExtensions
    {
        /// <summary>
        ///     Throws an exception if the response is not successful.
        /// </summary>
        /// <param name="source"></param>
        /// <exception cref="ManagerResponseException"></exception>
        public static void ThrowIfNotSuccess (this ManagerResponse? source, ManagerAction? action = null)
        {
            if (source == null || !source.IsSuccess())
                throw new ManagerResponseException(source, action);
        }
    }
}
