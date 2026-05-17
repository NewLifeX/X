using System.Collections.Concurrent;
using System.Net.Sockets;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Integration;

/// <summary>TCP JsonCodec+StandardCodec 集成测试固定装置</summary>
public class JsonCodecServerFixture : IDisposable
{
    public JsonCodecNetServer Server { get; }

    public JsonCodecServerFixture()
    {
        var server = new JsonCodecNetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

public class JsonCodecNetServer : NetServer<JsonCodecSession>
{
    /// <summary>服务端解码后收到的对象（IDictionary），线程安全</summary>
    public ConcurrentQueue<Object> ReceivedObjects { get; } = new();

    public JsonCodecNetServer()
    {
        Add<JsonCodec>();
        Add<StandardCodec>();
    }
}

public class JsonCodecSession : NetSession<JsonCodecNetServer>
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;

        Host.ReceivedObjects.Enqueue(msg);
        WriteLog("收到JSON对象：{0} 类型={1}", (msg as System.Collections.IDictionary)?.Count, msg.GetType().Name);

        SendMessage(msg);
    }
}

/// <summary>NetServer+NetClient 至少双编码器（JsonCodec + StandardCodec）集成测试</summary>
[Collection("Integration")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class JsonCodecIntegrationTests(JsonCodecServerFixture fixture) : IClassFixture<JsonCodecServerFixture>
{
    private NetClient CreateJsonClient()
    {
        var client = new NetClient($"tcp://127.0.0.1:{fixture.Server.Port}") { AutoReconnect = false };
        client.Add<JsonCodec>();
        client.Add<StandardCodec>();
        return client;
    }

    [Fact(DisplayName = "12-TCP+JsonCodec+StandardCodec 对象全流程：发送→内容回显→服务端确认解码")]
    public async Task Test12_JsonAndStandard_ObjectRoundtrip()
    {
        var obj = new Dictionary<String, Object>
        {
            ["name"] = "NewLife",
            ["count"] = 123,
            ["ok"] = true,
        };

        var serverBefore = fixture.Server.ReceivedObjects.Count;

        var wait = new TaskCompletionSource<Object>();
        using var client = CreateJsonClient();
        client.Received += (s, e) => wait.TrySetResult(e.Message!);

        client.Open();
        client.SendMessage(obj);
        var result = await wait.Task.WaitAsync(TimeSpan.FromSeconds(5));
        client.Close("done");

        // 客户端回显类型验证
        Assert.NotNull(result);
        Assert.True(result is IDictionary<String, Object?> || result is IDictionary<String, Object>,
            $"返回类型应为字典，实际：{result.GetType().FullName}");

        // 验证关键字段内容
        if (result is IDictionary<String, Object?> clientDict)
            Assert.Equal("NewLife", clientDict["name"]?.ToString());

        // 服务端确认收到了解码后的 JSON 对象
        await Task.Delay(50);
        Assert.True(fixture.Server.ReceivedObjects.Count > serverBefore, "服务端应已记录收到的 JSON 对象");
        var serverObj = fixture.Server.ReceivedObjects.LastOrDefault();
        Assert.NotNull(serverObj);
        Assert.True(serverObj is IDictionary<String, Object?> || serverObj is IDictionary<String, Object>,
            $"服务端接收对象应为字典，实际：{serverObj!.GetType().FullName}");

        if (serverObj is IDictionary<String, Object?> serverDict)
            Assert.Equal("NewLife", serverDict["name"]?.ToString());
    }
}
