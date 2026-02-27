using NewLife;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

[Collection("Net")]
public class EchoNetServerTests
{
    [Fact]
    public void Echo32Bytes()
    {
        using var server = new EchoNetServer { Port = 0 };
        server.Start();

        using var client = new NetUri($"tcp://127.0.0.1:{server.Port}").CreateRemote();
        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);

        var wait = new ManualResetEventSlim();
        Byte[]? received = null;
        client.Received += (s, e) =>
        {
            var packet = e.Packet;
            if (packet == null) return;

            received = packet.GetSpan().ToArray();
            wait.Set();
        };

        client.Open();
        _ = client.Send(payload);

        Assert.True(wait.Wait(3_000));
        Assert.NotNull(received);
        Assert.Equal(payload, received);
    }

    class EchoNetServer : NetServer<EchoSession>
    {
    }

    class EchoSession : NetSession<EchoNetServer>
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            var packet = e.Packet;
            if (packet == null || packet.Length == 0) return;

            Send(packet);
        }
    }
}
