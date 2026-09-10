using System.Net;
using System.Net.Sockets;
using System.Text;

internal sealed class FakeAmi : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _run;
    private readonly SemaphoreSlim _write = new(1);
    private StreamWriter? _writer;
    private TcpClient? _peer;
    public bool IgnorePing { get; set; }
    public TaskCompletionSource<bool> PingReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
    public FakeAmi() { _listener.Start(); _run = Run(); }
    public async Task Send(string text)
    {
        await _write.WaitAsync();
        try { await _writer!.WriteAsync(text); await _writer.FlushAsync(); }
        finally { _write.Release(); }
    }
    private async Task Run()
    {
        try
        {
            _peer = await _listener.AcceptTcpClientAsync(_stop.Token);
            _writer = new StreamWriter(_peer.GetStream(), new UTF8Encoding(false)) { NewLine = "\r\n" };
            using var reader = new StreamReader(_peer.GetStream());
            await Send("Asterisk Call Manager/9.0.0\r\n");
            var packet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadLineAsync(_stop.Token) is { } line)
            {
                if (line.Length > 0)
                {
                    var pos = line.IndexOf(':');
                    if (pos > 0) packet[line[..pos]] = line[(pos + 1)..].Trim();
                    continue;
                }
                if (packet.TryGetValue("Action", out var action))
                {
                    if (action.Equals("Ping", StringComparison.OrdinalIgnoreCase))
                    {
                        PingReceived.TrySetResult(true);
                        if (IgnorePing) { packet.Clear(); continue; }
                    }
                    var id = packet.GetValueOrDefault("ActionID", "");
                    await Send($"Response: Success\r\nActionID: {id}\r\nMessage: Accepted\r\n\r\n");
                }
                packet.Clear();
            }
        }
        catch (Exception) when (_stop.IsCancellationRequested) { }
        catch (IOException) { }
    }
    public async ValueTask DisposeAsync()
    {
        _stop.Cancel(); _peer?.Dispose(); _listener.Stop();
        await _run.WaitAsync(TimeSpan.FromSeconds(5)); _stop.Dispose(); _write.Dispose();
    }
}
