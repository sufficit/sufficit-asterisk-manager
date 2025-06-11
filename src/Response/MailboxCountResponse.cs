namespace Sufficit.Asterisk.Manager.Response
{
    /// <summary>
    ///     A MailboxCountResponse is sent in response to a MailboxCountAction and contains the number of old
    ///     and new messages in a mailbox.
    /// </summary>
    /// <seealso cref="Manager.Action.MailboxCountAction" />
    public class MailboxCountResponse : ManagerResponse
    {
        /// <summary>
        ///     Get/Set the name of the mailbox.
        /// </summary>
        public string Mailbox { get; set; } = default!;

        /// <summary>
        ///     Get/Set the number of new messages in the mailbox.
        /// </summary>
        public int NewMessages { get; set; }

        /// <summary>
        ///     Returns the number of old messages in the mailbox.
        /// </summary>
        public int OldMessages { get; set; }
    }
}