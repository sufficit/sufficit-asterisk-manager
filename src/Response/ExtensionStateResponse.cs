namespace Sufficit.Asterisk.Manager.Response
{
    public class ExtensionStateResponse : ManagerResponse
    {
        public string Exten { get; set; } = default!;

        public string Context { get; set; } = default!;

        public string Hint { get; set; } = default!;

        public int Status { get; set; }
    }
}