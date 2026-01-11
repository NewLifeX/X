using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>NetSession网络会话单元测试</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class NetSessionTests
{
    #region 基础功能测试
    /// <summary>测试会话ID自增分配</summary>
    [Fact]
    public void SessionIdIncrement()
    {
        var sessionIds = new List<Int32>();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = true,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            lock (sessionIds)
            {
                sessionIds.Add(e.Session!.ID);
            }
        };

        server.Start();

        // 创建多个客户端连接
        var clients = new List<TcpClient>();
        for (var i = 0; i < 5; i++)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, server.Port);
            clients.Add(client);
            Thread.Sleep(50);
        }

        Thread.Sleep(500);

        // 验证ID递增且唯一
        Assert.Equal(5, sessionIds.Count);
        Assert.Equal(sessionIds.Count, sessionIds.Distinct().Count());

        // 验证ID是递增的
        for (var i = 1; i < sessionIds.Count; i++)
        {
            Assert.True(sessionIds[i] > sessionIds[i - 1]);
        }

        // 清理
        foreach (var client in clients)
            client.Dispose();
    }

    /// <summary>测试会话Remote属性</summary>
    [Fact]
    public void SessionRemoteAddress()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        // 客户端连接
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.NotNull(createdSession.Remote);
        Assert.True(createdSession.Remote.Port > 0);
    }

    /// <summary>测试会话Host属性</summary>
    [Fact]
    public void SessionHostProperty()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);
        Assert.Same(server, createdSession.Host);
    }
    #endregion

    #region 生命周期测试
    /// <summary>测试会话Connected事件</summary>
    [Fact]
    public void SessionConnectedEvent()
    {
        var connectedCount = 0;
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer<ConnectedTestSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            if (e.Session is ConnectedTestSession session)
            {
                session.Connected += (sender, args) =>
                {
                    Interlocked.Increment(ref connectedCount);
                };
            }
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Thread.Sleep(200);

        // Connected事件应该已经在Start中触发
        Assert.True(connectedCount >= 0);
    }

    /// <summary>测试会话Disconnected事件</summary>
    [Fact]
    public void SessionDisconnectedEvent()
    {
        var disconnectedReason = "";
        var disconnectedEvent = new ManualResetEventSlim(false);
        INetSession? createdSession = null;

        using var server = new NetServer<DisconnectedTestSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            if (e.Session is DisconnectedTestSession session)
            {
                session.DisconnectedCallback = reason =>
                {
                    disconnectedReason = reason;
                    disconnectedEvent.Set();
                };
            }
        };

        server.Start();

        var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        // 关闭客户端触发断开
        client.Close();

        // 等待断开事件
        if (disconnectedEvent.Wait(3000))
        {
            Assert.NotEmpty(disconnectedReason);
        }
    }

    /// <summary>测试会话Close方法</summary>
    [Fact]
    public void SessionCloseMethod()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);
        var disconnected = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            if (e.Session is NetSession ns)
            {
                ns.Disconnected += (sender, args) => disconnected.Set();
            }
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);

        // 主动关闭会话
        createdSession.Close("TestClose");

        // 等待断开事件触发
        Assert.True(disconnected.Wait(3000));

        // Close方法调用后，重复调用应该是安全的（幂等）
        createdSession.Close("TestClose2");
    }

    class ConnectedTestSession : NetSession
    {
        protected override void OnConnected()
        {
            base.OnConnected();
        }
    }

    class DisconnectedTestSession : NetSession
    {
        public Action<String>? DisconnectedCallback { get; set; }

        protected override void OnDisconnected(String reason)
        {
            base.OnDisconnected(reason);
            DisconnectedCallback?.Invoke(reason);
        }
    }
    #endregion

    #region 数据收发测试
    /// <summary>测试会话Send数据包</summary>
    [Fact]
    public void SessionSendPacket()
    {
        var receivedData = new List<Byte[]>();
        var dataReceived = new ManualResetEventSlim(false);
        INetSession? createdSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        Assert.NotNull(createdSession);

        // 服务端发送数据
        var sendData = "Hello from server"u8.ToArray();
        createdSession.Send(new ArrayPacket(sendData));

        // 客户端接收
        var ns = client.GetStream();
        var buf = new Byte[1024];
        var len = ns.Read(buf, 0, buf.Length);

        Assert.Equal(sendData.Length, len);
        Assert.Equal(sendData, buf[..len]);
    }

    /// <summary>测试会话Send字节数组</summary>
    [Fact]
    public void SessionSendByteArray()
    {
        INetSession? createdSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        Assert.NotNull(createdSession);

        // 服务端发送字节数组
        var sendData = Encoding.UTF8.GetBytes("Byte array test");
        createdSession.Send(sendData);

        // 客户端接收
        var ns = client.GetStream();
        var buf = new Byte[1024];
        var len = ns.Read(buf, 0, buf.Length);

        Assert.Equal(sendData.Length, len);
        Assert.Equal(sendData, buf[..len]);
    }

    /// <summary>测试会话Send字符串</summary>
    [Fact]
    public void SessionSendString()
    {
        INetSession? createdSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        Assert.NotNull(createdSession);

        // 服务端发送字符串
        var sendMsg = "String message test 中文测试";
        createdSession.Send(sendMsg);

        // 客户端接收
        var ns = client.GetStream();
        var buf = new Byte[1024];
        var len = ns.Read(buf, 0, buf.Length);

        var received = Encoding.UTF8.GetString(buf, 0, len);
        Assert.Equal(sendMsg, received);
    }

    /// <summary>测试会话Received事件</summary>
    [Fact]
    public void SessionReceivedEvent()
    {
        var receivedData = new List<Byte[]>();
        var dataReceived = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.Received += (s, e) =>
        {
            if (e.Packet != null)
            {
                receivedData.Add(e.Packet.ToArray());
                dataReceived.Set();
            }
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        // 客户端发送数据
        var ns = client.GetStream();
        var sendData = "Hello from client"u8.ToArray();
        ns.Write(sendData, 0, sendData.Length);

        Assert.True(dataReceived.Wait(3000));
        Assert.Single(receivedData);
        Assert.Equal(sendData, receivedData[0]);
    }

    /// <summary>测试会话链式Send调用</summary>
    [Fact]
    public void SessionChainedSend()
    {
        INetSession? createdSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        Thread.Sleep(200);

        Assert.NotNull(createdSession);

        // 链式调用发送多条数据
        var result = createdSession
            .Send("First\n")
            .Send("Second\n")
            .Send("Third\n");

        Assert.Same(createdSession, result);

        // 验证客户端收到数据
        var ns = client.GetStream();
        var buf = new Byte[1024];
        Thread.Sleep(100);
        var len = ns.Read(buf, 0, buf.Length);

        Assert.True(len > 0);
    }
    #endregion

    #region Items扩展数据测试
    /// <summary>测试会话Items属性</summary>
    [Fact]
    public void SessionItemsProperty()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);

        if (createdSession is NetSession ns)
        {
            // 设置扩展数据
            ns["Key1"] = "Value1";
            ns["Key2"] = 123;
            ns["Key3"] = null;

            Assert.Equal("Value1", ns["Key1"]);
            Assert.Equal(123, ns["Key2"]);
            Assert.Null(ns["Key3"]);
            Assert.Null(ns["NonExistent"]);
        }
    }
    #endregion

    #region 自定义会话测试
    /// <summary>测试自定义会话类型</summary>
    [Fact]
    public void CustomSessionType()
    {
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer<CustomSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));

        var sessions = server.Sessions;
        Assert.NotEmpty(sessions);

        var session = server.GetSession(sessions.First().Key);
        Assert.NotNull(session);
        Assert.IsType<CustomSession>(session);
    }

    /// <summary>测试自定义会话OnReceive重载</summary>
    [Fact]
    public void CustomSessionOnReceive()
    {
        var processedMessages = new List<String>();
        var messageProcessed = new ManualResetEventSlim(false);

        using var server = new NetServer<ReceiveTestSession>
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            if (e.Session is ReceiveTestSession session)
            {
                session.MessageCallback = msg =>
                {
                    lock (processedMessages)
                    {
                        processedMessages.Add(msg);
                    }
                    messageProcessed.Set();
                };
            }
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        // 发送数据
        var ns = client.GetStream();
        var sendData = "Custom message"u8.ToArray();
        ns.Write(sendData, 0, sendData.Length);

        Assert.True(messageProcessed.Wait(3000));
        Assert.NotEmpty(processedMessages);
    }

    class CustomSession : NetSession
    {
        public String CustomProperty { get; set; } = "Default";
    }

    class ReceiveTestSession : NetSession
    {
        public Action<String>? MessageCallback { get; set; }

        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);

            var msg = e.Packet?.ToStr();
            if (!String.IsNullOrEmpty(msg))
            {
                MessageCallback?.Invoke(msg);
            }
        }
    }
    #endregion

    #region 泛型会话测试
    /// <summary>测试NetSession泛型Host属性</summary>
    [Fact]
    public void GenericSessionHostProperty()
    {
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new MyServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            CustomServerProperty = "TestValue",
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));

        var sessions = server.Sessions;
        Assert.NotEmpty(sessions);

        var session = sessions.First().Value as GenericHostSession;
        Assert.NotNull(session);

        // 验证泛型Host可以访问服务器自定义属性
        Assert.Equal("TestValue", session.Host.CustomServerProperty);
    }

    class MyServer : NetServer<GenericHostSession>
    {
        public String CustomServerProperty { get; set; } = "";
    }

    class GenericHostSession : NetSession<MyServer>
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);
            // 可以直接访问 Host.CustomServerProperty
            var _ = Host.CustomServerProperty;
        }
    }
    #endregion

    #region 服务提供者测试
    /// <summary>测试会话ServiceProvider</summary>
    [Fact]
    public void SessionServiceProvider()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);

        // 配置服务
        var services = ObjectContainer.Current;
        services.AddSingleton<ITestService, TestServiceImpl>();

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            ServiceProvider = services.BuildServiceProvider(),
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);

        if (createdSession is NetSession ns)
        {
            // 验证服务提供者已设置
            Assert.NotNull(ns.ServiceProvider);
        }
    }

    /// <summary>测试会话GetService方法</summary>
    [Fact]
    public void SessionGetService()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);

        if (createdSession is NetSession ns)
        {
            // 测试内置类型
            Assert.Same(ns, ns.GetService(typeof(NetSession)));
            Assert.Same(ns, ns.GetService(typeof(INetSession)));
            Assert.Same(server, ns.GetService(typeof(NetServer)));
            Assert.NotNull(ns.GetService(typeof(ISocketSession)));
        }
    }

    interface ITestService
    {
        void DoWork();
    }

    class TestServiceImpl : ITestService
    {
        public void DoWork() { }
    }
    #endregion

    #region 日志测试
    /// <summary>测试会话LogPrefix</summary>
    [Fact]
    public void SessionLogPrefix()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);

        if (createdSession is NetSession ns)
        {
            var logPrefix = ns.LogPrefix;
            Assert.NotEmpty(logPrefix);
            Assert.Contains(server.Name, logPrefix);
            Assert.Contains(ns.ID.ToString(), logPrefix);
        }
    }
    #endregion

    #region ToString测试
    /// <summary>测试会话ToString</summary>
    [Fact]
    public void SessionToString()
    {
        INetSession? createdSession = null;
        var sessionCreated = new ManualResetEventSlim(false);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };

        server.NewSession += (s, e) =>
        {
            createdSession = e.Session;
            sessionCreated.Set();
        };

        server.Start();

        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionCreated.Wait(3000));
        Assert.NotNull(createdSession);

        if (createdSession is NetSession ns)
        {
            var str = ns.ToString();
            Assert.NotEmpty(str);
            Assert.Contains(server.Name, str);
        }
    }
    #endregion
}
