using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.Asterisk.Manager.Connection
{
    public class DisconnectEventArgs : EventArgs
    {
        public string Cause { get; set; } = default!;

        public bool IsPermanent { get; set; }
    }
}
