namespace Sufficit.Asterisk.Manager.Response
{
    /// <summary>
    ///     A MailboxStatusResponse is sent in response to a MailboxStatusAction and indicates if a set
    ///     of mailboxes contains waiting messages.
    /// </summary>
    /// <seealso cref="Manager.Action.MailboxStatusAction" />
    public class MailboxStatusResponse : ManagerResponse
    {
        /// <summary>
        ///     Get/Set the names of the mailboxes, separated by ",".
        /// </summary>
        public string Mailbox { get; set; } = default!;

        /// <summary>
        ///     Get/Set true if at least one of the given mailboxes contains new messages, false otherwise.
        /// </summary>
        public bool Waiting { get; set; }
    }
}