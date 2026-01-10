using System.Net.Http.Headers;
using NewLife;
using NewLife.Http;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Remoting;

public class ApiHttpClientTests : DisposeBase
{
    private readonly ApiServer _Server;
    private readonly String _Address;
    private readonly IApiClient _Client;

    public ApiHttpClientTests()
    {
        _Server = new ApiServer(12347)
        {
            //Log = XTrace.Log,
            //EncoderLog = XTrace.Log,
        };
        _Server.Handler = new TokenApiHandler { Host = _Server };
        _Server.Start();

        _Address = "http://127.0.0.1:12347";

        //_Client = new ApiHttpClient();
        //_Client.Add("addr1", new Uri("http://127.0.0.1:12347"));
        _Client = new ApiHttpClient(_Address);
    }

    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _Server.TryDispose();
    }

    #region 基础测试
    [Fact(DisplayName = "基础Api测试")]
    public async Task BasicTest()
    {
        var apis = await _Client.InvokeAsync<String[]>("api/all");
        Assert.NotNull(apis);
        Assert.Equal(2, apis.Length);
        Assert.Equal("String[] Api/All()", apis[0]);
        Assert.Equal("Object Api/Info(String state)", apis[1]);
        //Assert.Equal("Packet Api/Info2(Packet state)", apis[2]);
    }

    [Fact(DisplayName = "参数测试")]
    public async Task InfoTest()
    {
        var state = Rand.NextString(8);
        var state2 = Rand.NextString(8);

        var infs = await _Client.InvokeAsync<IDictionary<String, Object>>("api/info", new { state, state2 });
        Assert.NotNull(infs);
        Assert.Equal(Environment.MachineName, infs["MachineName"]);
        //Assert.Equal(Environment.UserName, infs["UserName"]);

        Assert.Equal(state, infs["state"]);
        Assert.Null(infs["state2"]);
    }

    //[Fact(DisplayName = "二进制测试")]
    //public async void Info2Test()
    //{
    //    var buf = Rand.NextBytes(32);

    //    var pk = await _Client.InvokeAsync<Packet>("api/info2", buf);
    //    Assert.NotNull(pk);
    //    Assert.True(pk.Total > buf.Length);
    //    Assert.Equal(buf, pk.Slice(pk.Total - buf.Length, -1).ToArray());
    //}

    [Fact(DisplayName = "异常请求")]
    public async Task ErrorTest()
    {
        var ex = await Assert.ThrowsAsync<ApiException>(() => _Client.InvokeAsync<Object>("api/info3"));

        Assert.NotNull(ex);
        Assert.Equal(404, ex.Code);
        //Assert.True(ex.Message.EndsWith("无法找到名为[api/info3]的服务！"));
        Assert.EndsWith("无法找到名为[api/info3]的服务！", ex.Message);
    }

    [Fact(DisplayName = "同步调用测试")]
    public void SyncInvokeTest()
    {
        var client = new ApiHttpClient(_Address);
        var state = Rand.NextString(8);

        var infs = client.Get<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(infs);
        Assert.Equal(state, infs["state"]);

        infs = client.Post<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(infs);
    }

    [Fact(DisplayName = "各种Http方法测试")]
    public async Task HttpMethodsTest()
    {
        var client = new ApiHttpClient(_Address);
        client.Timeout = 3_000;
        var state = Rand.NextString(8);

        // GET
        var rs = await client.GetAsync<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(rs);
        Assert.Equal(state, rs["state"]);

        // POST
        rs = await client.PostAsync<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(rs);

        // PUT
        rs = await client.PutAsync<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(rs);

        // PATCH
        rs = await client.PatchAsync<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(rs);

        // DELETE
        rs = await client.DeleteAsync<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(rs);
    }
    #endregion

    #region Token令牌测试
    [Theory(DisplayName = "Token令牌测试")]
    [InlineData("12345678", "ABCDEFG")]
    [InlineData("ABCDEFG", "12345678")]
    public async Task TokenTest(String token, String state)
    {
        var client = new ApiHttpClient(_Address) { Token = token };
        var ac = client as IApiClient;

        var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info", new { state });
        Assert.NotNull(infs);
        Assert.Equal(token, infs["token"]);

        // 另一个客户端，共用令牌，应该可以拿到上一次状态数据
        var client2 = new ApiHttpClient(_Address) { Token = token };

        infs = await client2.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);
    }

    [Fact(DisplayName = "Authentication属性测试")]
    public async Task AuthenticationTest()
    {
        var client = new ApiHttpClient(_Address)
        {
            Authentication = new AuthenticationHeaderValue("Bearer", "test_auth_token")
        };

        var infs = await client.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);
        Assert.Equal("test_auth_token", infs["token"]);
    }

    [Fact(DisplayName = "Authentication优先于Token测试")]
    public async Task AuthenticationPriorityTest()
    {
        // Authentication 属性应该覆盖 Token
        var client = new ApiHttpClient(_Address)
        {
            Token = "token_value",
            Authentication = new AuthenticationHeaderValue("Bearer", "auth_value")
        };

        var infs = await client.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);
        // 当 Authentication 为空时才使用 Token，这里 Authentication 不为空，所以用 Authentication
        Assert.Equal("auth_value", infs["token"]);
    }

    [Fact(DisplayName = "服务节点独立Token测试")]
    public void ServiceTokenTest()
    {
        // 地址中带 Token 参数
        var client = new ApiHttpClient();
        var svc = client.Add("test", _Address + "#token=node_token_123");

        Assert.Equal("node_token_123", svc.Token);
        Assert.Equal("http://127.0.0.1:12347/", svc.Address + "");
    }
    #endregion

    #region 负载均衡测试
    [Fact]
    public void SlaveTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:10000,http://127.0.0.1:20000," + _Address)
        {
            Timeout = 3_000
        };
        var ac = client as IApiClient;

        var infs = ac.Invoke<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);
    }

    [Fact]
    public async Task SlaveAsyncTest()
    {
        var filter = new TokenHttpFilter
        {
            UserName = "test",
            Password = "",
        };
        var client = new ApiHttpClient("http://127.0.0.1:10001,http://127.0.0.1:20001,http://star.newlifex.com:6600")
        {
            Filter = filter,
            Timeout = 3_000
        };

        var rs = await client.PostAsync<Object>("config/getall", new { appid = "starweb" });
        Assert.NotNull(rs);

        var ss = client.Services;
        Assert.Equal(3, ss.Count);
        Assert.Equal(1, ss[0].Times);
        Assert.Equal(1, ss[0].Errors);
        Assert.Equal(1, ss[1].Times);
        Assert.Equal(1, ss[1].Errors);
        Assert.Equal(1, ss[2].Times);
        Assert.Equal(0, ss[2].Errors);
    }

    [Fact(DisplayName = "加权轮询测试")]
    public async Task RoundRobinTest()
    {
#pragma warning disable CS0618 // 类型或成员已过时
        var client = new ApiHttpClient("test1=3*http://127.0.0.1:10000,test2=7*http://127.0.0.1:20000,")
        {
            RoundRobin = true,
            Timeout = 3_000,
            Log = XTrace.Log,
        };
#pragma warning restore CS0618 // 类型或成员已过时

        Assert.Equal(2, client.Services.Count);

        // 再加两个
        client.Add("test3", "2*" + _Address);
        client.Add("test4", "1*" + _Address);

        Assert.Equal(4, client.Services.Count);

        {
            var svc = client.Services[0];
            Assert.Equal("test1", svc.Name);
            Assert.Equal(3, svc.Weight);
            Assert.Equal("http://127.0.0.1:10000/", svc.Address + "");

            svc = client.Services[1];
            Assert.Equal("test2", svc.Name);
            Assert.Equal(7, svc.Weight);
            Assert.Equal("http://127.0.0.1:20000/", svc.Address + "");

            svc = client.Services[2];
            Assert.Equal("test3", svc.Name);
            Assert.Equal(2, svc.Weight);
            Assert.Equal(_Address + "/", svc.Address + "");
        }

        var ac = client as IApiClient;

        {
            var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
        }
        {
            var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
        }
        {
            var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
        }
        {
            var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
        }

        // 判断结果
        {
            var svc = client.Services[0];
            Assert.Null(svc.Client);
            Assert.True(svc.NextTime > DateTime.Now.AddSeconds(55));
            Assert.Equal(1, svc.Times);
        }
        {
            var svc = client.Services[1];
            Assert.Null(svc.Client);
            Assert.True(svc.NextTime > DateTime.Now.AddSeconds(55));
            Assert.Equal(1, svc.Times);
        }
        {
            var svc = client.Services[2];
            Assert.NotNull(svc.Client);
            Assert.True(svc.NextTime.Year < 2000);
            Assert.Equal(3, svc.Times);
        }
        {
            var svc = client.Services[3];
            Assert.NotNull(svc.Client);
            Assert.True(svc.NextTime.Year < 2000);
            Assert.Equal(1, svc.Times);
        }
    }

    [Fact(DisplayName = "负载均衡模式切换测试")]
    public void LoadBalanceModeTest()
    {
        var client = new ApiHttpClient(_Address);

        // 默认故障转移模式
        Assert.Equal(LoadBalanceMode.Failover, client.LoadBalanceMode);
        Assert.IsType<FailoverLoadBalancer>(client.LoadBalancer);

        // 切换到轮询模式
        client.LoadBalanceMode = LoadBalanceMode.RoundRobin;
        Assert.Equal(LoadBalanceMode.RoundRobin, client.LoadBalanceMode);
        Assert.IsType<WeightedRoundRobinLoadBalancer>(client.LoadBalancer);

        // 切换到竞速模式
        client.LoadBalanceMode = LoadBalanceMode.Race;
        Assert.Equal(LoadBalanceMode.Race, client.LoadBalanceMode);
        Assert.IsType<RaceLoadBalancer>(client.LoadBalancer);

        // 切回故障转移
        client.LoadBalanceMode = LoadBalanceMode.Failover;
        Assert.Equal(LoadBalanceMode.Failover, client.LoadBalanceMode);
        Assert.IsType<FailoverLoadBalancer>(client.LoadBalancer);
    }

    [Fact(DisplayName = "故障转移模式测试")]
    public async Task FailoverModeTest()
    {
        // 两个不可用地址 + 一个可用地址
        var client = new ApiHttpClient($"http://127.0.0.1:19999,http://127.0.0.1:29999,{_Address}")
        {
            Timeout = 2_000,
            LoadBalanceMode = LoadBalanceMode.Failover,
        };

        Assert.Equal(3, client.Services.Count);

        // 第一次调用应该失败两次后成功
        var infs = await client.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);

        // 检查服务状态
        var svc0 = client.Services[0];
        var svc1 = client.Services[1];
        var svc2 = client.Services[2];

        Assert.Equal(1, svc0.Times);
        Assert.Equal(1, svc0.Errors);
        Assert.Equal(1, svc1.Times);
        Assert.Equal(1, svc1.Errors);
        Assert.Equal(1, svc2.Times);
        Assert.Equal(0, svc2.Errors);

        // 当前使用的服务应该是可用的那个
        Assert.Equal(svc2, client.Current);
    }

    [Fact(DisplayName = "屏蔽时间测试")]
    public void ShieldingTimeTest()
    {
        var client = new ApiHttpClient(_Address)
        {
            ShieldingTime = 120
        };

        Assert.Equal(120, client.ShieldingTime);
        Assert.Equal(120, client.LoadBalancer.ShieldingTime);

        // 切换模式后屏蔽时间应该保留
        client.LoadBalanceMode = LoadBalanceMode.RoundRobin;
        Assert.Equal(120, client.LoadBalancer.ShieldingTime);
    }
    #endregion

    #region 服务地址测试
    [Fact(DisplayName = "SetServer地址解析测试")]
    public void SetServerTest()
    {
        var client = new ApiHttpClient();

        // 单个地址
        client.SetServer("http://127.0.0.1:8080");
        Assert.Single(client.Services);
        Assert.Equal("http://127.0.0.1:8080/", client.Services[0].Address + "");

        // 多个地址
        client.SetServer("http://127.0.0.1:8080,http://127.0.0.1:8081,http://127.0.0.1:8082");
        Assert.Equal(3, client.Services.Count);

        // 相同地址不会重复设置
        client.SetServer("http://127.0.0.1:8080,http://127.0.0.1:8081,http://127.0.0.1:8082");
        Assert.Equal(3, client.Services.Count);

        // 不同地址会替换
        client.SetServer("http://127.0.0.1:9090");
        Assert.Single(client.Services);
        Assert.Equal("http://127.0.0.1:9090/", client.Services[0].Address + "");
    }

    [Fact(DisplayName = "AddServer带权重测试")]
    public void AddServerWithWeightTest()
    {
        var client = new ApiHttpClient();

        var svcs = client.AddServer("api", "http://127.0.0.1:8080,http://127.0.0.1:8081", 5);
        Assert.Equal(2, svcs.Count);
        Assert.Equal(5, svcs[0].Weight);
        Assert.Equal(5, svcs[1].Weight);

        Assert.Equal("api1", svcs[0].Name);
        Assert.Equal("api2", svcs[1].Name);
    }

    [Theory(DisplayName = "地址解析带名称和权重测试")]
    [InlineData("master=3*http://127.0.0.1:8080", "master", 3, "http://127.0.0.1:8080/")]
    [InlineData("slave=7*http://127.0.0.1:8081", "slave", 7, "http://127.0.0.1:8081/")]
    [InlineData("http://127.0.0.1:8082", "test", 1, "http://127.0.0.1:8082/")]
    [InlineData("5*http://127.0.0.1:8083", "test", 5, "http://127.0.0.1:8083/")]
    public void ParseAddressTest(String address, String expectedName, Int32 expectedWeight, String expectedUrl)
    {
        var client = new ApiHttpClient();
        var svc = client.Add("test", address);

        if (address.Contains("="))
            Assert.Equal(expectedName, svc.Name);
        Assert.Equal(expectedWeight, svc.Weight);
        Assert.Equal(expectedUrl, svc.Address + "");
    }

    [Fact(DisplayName = "地址解析带Token测试")]
    public void ParseAddressWithTokenTest()
    {
        var client = new ApiHttpClient();
        var svc = client.Add("test", "http://127.0.0.1:8080#token=my_secret_token");

        Assert.Equal("my_secret_token", svc.Token);
        Assert.Equal("http://127.0.0.1:8080/", svc.Address + "");
    }

    [Fact(DisplayName = "Add方法Uri重载测试")]
    public void AddUriTest()
    {
        var client = new ApiHttpClient();
        var svc = client.Add("myservice", new Uri("http://127.0.0.1:8080/api"));

        Assert.Equal("myservice", svc.Name);
        Assert.Equal("http://127.0.0.1:8080/api", svc.Address + "");
    }
    #endregion

    #region 其它功能测试
    [Fact(DisplayName = "超时设置测试")]
    public void TimeoutTest()
    {
        var client = new ApiHttpClient(_Address)
        {
            Timeout = 5_000
        };

        Assert.Equal(5_000, client.Timeout);

        // 获取客户端验证超时设置
        var svc = client.Services[0];
        var httpClient = client.EnsureClient(svc);
        Assert.Equal(TimeSpan.FromMilliseconds(5_000), httpClient.Timeout);
    }

    [Fact(DisplayName = "OnRequest事件测试")]
    public async Task OnRequestEventTest()
    {
        var client = new ApiHttpClient(_Address);
        var eventTriggered = false;
        HttpRequestMessage? capturedRequest = null;

        client.OnRequest += (sender, e) =>
        {
            eventTriggered = true;
            capturedRequest = e.Request;
            e.Request.Headers.Add("X-Custom-Header", "test_value");
        };

        var infs = await client.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);
        Assert.True(eventTriggered);
        Assert.NotNull(capturedRequest);
    }

    [Fact(DisplayName = "OnCreateClient事件测试")]
    public async Task OnCreateClientEventTest()
    {
        var client = new ApiHttpClient(_Address);
        var eventTriggered = false;

        client.OnCreateClient += (sender, e) =>
        {
            eventTriggered = true;
            Assert.NotNull(e.Client);
        };

        var infs = await client.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);
        Assert.True(eventTriggered);
    }

    [Fact(DisplayName = "DefaultUserAgent测试")]
    public void DefaultUserAgentTest()
    {
        var client = new ApiHttpClient(_Address)
        {
            DefaultUserAgent = "MyApp/1.0"
        };

        Assert.Equal("MyApp/1.0", client.DefaultUserAgent);
    }

    [Fact(DisplayName = "Source属性测试")]
    public async Task SourcePropertyTest()
    {
        var client = new ApiHttpClient();
        client.Add("primary", _Address);

        var infs = await client.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);

        // 调用后 Source 应该记录当前使用的服务名
        Assert.Equal("primary", client.Source);
    }

    [Fact(DisplayName = "Current属性测试")]
    public async Task CurrentPropertyTest()
    {
        var client = new ApiHttpClient(_Address);

        // 调用前 Current 为 null
        Assert.Null(client.Current);

        var infs = await client.GetAsync<IDictionary<String, Object>>("api/info");
        Assert.NotNull(infs);

        // 调用成功后 Current 应该指向使用的服务
        Assert.NotNull(client.Current);
        Assert.Equal(_Address + "/", client.Current.Address + "");
    }
    #endregion

    #region Filter测试
    [Fact]
    public async Task FilterTest()
    {
        var filter = new TokenHttpFilter
        {
            UserName = "test",
            Password = "",
        };

        var client = new ApiHttpClient("http://star.newlifex.com:6600")
        {
            Filter = filter,

            Log = XTrace.Log,
        };

        var rs = await client.PostAsync<Object>("config/getall", new { appid = "starweb" });

        Assert.NotNull(rs);
        Assert.NotNull(filter.Token);
    }
    #endregion

    #region CodeName/DataName测试
    [Fact(DisplayName = "自定义CodeName测试")]
    public void CustomCodeNameTest()
    {
        var client = new ApiHttpClient(_Address)
        {
            CodeName = "status"
        };

        Assert.Equal("status", client.CodeName);
    }

    [Fact(DisplayName = "自定义DataName测试")]
    public void CustomDataNameTest()
    {
        var client = new ApiHttpClient(_Address)
        {
            DataName = "result"
        };

        Assert.Equal("result", client.DataName);
    }
    #endregion

    #region JsonHost测试
    [Fact(DisplayName = "JsonHost属性测试")]
    public void JsonHostTest()
    {
        var client = new ApiHttpClient(_Address);

        // 默认为 null，使用全局 JsonHelper.Default
        Assert.Null(client.JsonHost);

        // 设置自定义 JsonHost
        var jsonHost = JsonHelper.Default;
        client.JsonHost = jsonHost;
        Assert.Equal(jsonHost, client.JsonHost);
    }
    #endregion
}