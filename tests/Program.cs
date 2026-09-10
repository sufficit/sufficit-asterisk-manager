using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.IO;
using Sufficit.Asterisk.Manager;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Connection;
using Sufficit.Asterisk.Manager.Events;

var passed = 0;
void Check(bool value, string text) { if (!value) throw new Exception(text); Console.WriteLine("PASS " + text); passed++; }
async Task Until(Func<bool> condition)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!condition()) await Task.Delay(10, timeout.Token);
}
ManagerConnectionParameters Options(int port) => new()
{
    Address = "127.0.0.1", Port = (uint)port, Username = "test", Password = "test",
    UseMD5Authentication = false, KeepAlive = false, PingInterval = 0,
    ReceivePacketCapacity = 16, ReceiveLineCapacity = 256,
    ReceiveMaxLineChars = 128, ReceiveMaxPacketChars = 512
};

new ManagerConnectionParameters().ValidateReceiveLimits();
Check(new ManagerConnectionParameters().ReceivePacketCapacity == 0, "legacy defaults unchanged");
try { new ManagerConnectionParameters { ReceivePacketCapacity = 2 }.ValidateReceiveLimits(); throw new Exception("Accepted partial limits"); }
catch (ArgumentOutOfRangeException) { passed++; }
Check(!Options(1).Equals(new ManagerConnectionParameters { Port = 1 }), "receive limits participate in equality");

await using (var server = new FakeAmi())
using (var connection = new ManagerConnection(Options(server.Port)))
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await connection.Login(deadline.Token);
    var response = await connection.SendActionAsync(new PingAction(), deadline.Token);
    Check(connection.IsAuthenticated && response.Exception is null, "login and correlated action response");
    await server.Send("Event: PeerSta");
    await server.Send("tus\r\nPeer: PJSIP/test\r\nPeerStatus: Registered\r\n\r\n");
    Check(!connection.ReceiveLimitExceeded, "fragmented valid frame accepted");
    using var legacy = new ManagerEventSubscriptions();
    try { connection.Use(legacy); throw new Exception("Accepted lossy events"); }
    catch (ArgumentException) { passed++; }
    await server.Send(new string('x', 129));
    await Until(() => connection.ReceiveLimitExceeded && !connection.IsConnected);
    Check(connection.ReceiveLimitExceeded, "oversized unterminated line quarantines connection");
    try { await connection.Connect(deadline.Token); throw new Exception("Reused quarantined connection"); }
    catch (InvalidOperationException) { passed++; }
}

await using (var server = new FakeAmi())
using (var connection = new ManagerConnection(Options(server.Port)))
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await connection.Login(deadline.Token);
    await server.Send("Response: Follows\r\n" + string.Concat(Enumerable.Repeat("output: " + new string('a', 60) + "\r\n", 10)));
    await Until(() => connection.ReceiveLimitExceeded);
    Check(!connection.IsConnected, "oversized command response closes socket");
}

await using (var server = new FakeAmi { IgnorePing = true })
using (var connection = new ManagerConnection(Options(server.Port)))
using (var subscriptions = new ManagerEventSubscriptions(1, true))
using (var gate = new ManualResetEventSlim())
{
    var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); var count = 0;
    subscriptions.On<PeerStatusEvent>((_, _) => { Interlocked.Increment(ref count); entered.TrySetResult(true); gate.Wait(TimeSpan.FromSeconds(5)); });
    connection.Use(subscriptions);
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await connection.Login(deadline.Token);
    var pending = connection.SendActionAsync(new PingAction(), deadline.Token);
    await server.PingReceived.Task.WaitAsync(deadline.Token);
    const string evt = "Event: PeerStatus\r\nPeer: PJSIP/test\r\nPeerStatus: Registered\r\n\r\n";
    try
    {
        await server.Send(evt); await entered.Task.WaitAsync(deadline.Token);
        await server.Send(evt + evt + evt);
        await Until(() => connection.ReceiveLimitExceeded);
        Check(!connection.IsConnected, "slow subscriber causes explicit quarantine");
        try { var result = await pending.WaitAsync(TimeSpan.FromSeconds(1)); Check(result.Exception is not null, "pending action failed on overload"); }
        catch (InvalidOperationException) { passed++; Console.WriteLine("PASS pending action failed on overload"); }
        catch (NotConnectedException) { passed++; Console.WriteLine("PASS pending action failed on disconnect"); }
    }
    finally { gate.Set(); }
    await Task.Delay(100);
    Check(count == 1, "queued events of faulted connection not dispatched");
}

var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
try
{
    using var peer = new TcpClient(); var connect = peer.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
    using var socket = await listener.AcceptSocketAsync(); await connect;
    await using var handler = new AISingleSocketHandler(NullLogger.Instance,
        new AGISocketOptions { ReceiveLineCapacity = 1, ReceiveMaxLineChars = 64 }, socket);
    await peer.GetStream().WriteAsync(Encoding.ASCII.GetBytes("one\r\ntwo\r\nthree\r\n"));
    await Until(() => handler.ReceiveLimitExceeded);
    Check(handler.ReceiveLimitExceeded, "raw line queue overflow bounded before packet parser");
}
finally { listener.Stop(); }

// A new instance after failure can log in; never carry old frames into a new generation.
await using (var server = new FakeAmi())
using (var connection = new ManagerConnection(Options(server.Port)))
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await connection.Login(deadline.Token);
    Check(connection.IsAuthenticated && !connection.ReceiveLimitExceeded, "fresh connection after fault works");
}
await using (var server = new FakeAmi())
{
    var config = new AMIProviderOptions
    {
        Address = "127.0.0.1", Port = (uint)server.Port, Username = "test", Password = "test", Title = "canary",
        UseMD5Authentication = false, PingInterval = 0, OldVersion = false,
        ReceivePacketCapacity = 16, ReceiveLineCapacity = 256, ReceiveMaxLineChars = 128, ReceiveMaxPacketChars = 512
    };
    using var provider = new AsteriskManagerProvider(Microsoft.Extensions.Options.Options.Create(config), NullLogger<AsteriskManagerProvider>.Instance);
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var old = (ManagerConnection)await provider.ConnectAsync(false, deadline.Token);
    old.Disconnect("simulated network interruption", false);
    Check(old.RequiresReplacement && !old.ReceiveLimitExceeded, "network loss also ends receive generation");
    await using var nextServer = new FakeAmi(); config.Port = (uint)nextServer.Port;
    var next = await provider.ConnectAsync(false, deadline.Token);
    Check(!ReferenceEquals(old, next) && next.IsAuthenticated, "provider replaces ended generation and authenticates");
}
Console.WriteLine($"PASS {passed} checks");
