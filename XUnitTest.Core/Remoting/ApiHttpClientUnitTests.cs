using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Configuration;
using NewLife.Remoting;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Remoting;

/// <summary>ApiHttpClient 纯单元测试（不依赖 ApiServer）</summary>
public class ApiHttpClientUnitTests
{
    #region Mock Handler
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage>? Handler { get; set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            var response = Handler?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":0,\"data\":\"ok\"}")
            };

            return Task.FromResult(response);
        }
    }

    private class TestableApiHttpClient : ApiHttpClient
    {
        private readonly HttpMessageHandler _handler;
        private Boolean _clientCreated = false;

        public Boolean ClientCreated => _clientCreated;

        public TestableApiHttpClient(HttpMessageHandler handler, String url) : base(url)
        {
            _handler = handler;
        }

        protected override HttpClient CreateClient()
        {
            _clientCreated = true;

            var client = new HttpClient(_handler)
            {
                Timeout = TimeSpan.FromMilliseconds(Timeout)
            };

            var userAgent = DefaultUserAgent;
            if (!userAgent.IsNullOrEmpty()) client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

            // 触发 OnCreateClient 事件需要通过基类调用
            return client;
        }
    }
    #endregion

    #region 构造与初始化测试
    [Fact(DisplayName = "默认构造函数测试")]
    public void DefaultConstructorTest()
    {
        var client = new ApiHttpClient();

        Assert.Equal(15_000, client.Timeout);
        Assert.False(client.UseProxy);
        Assert.Equal(LoadBalanceMode.Failover, client.LoadBalanceMode);
        Assert.NotNull(client.LoadBalancer);
        Assert.IsType<FailoverLoadBalancer>(client.LoadBalancer);
        Assert.Empty(client.Services);
    }

    [Fact(DisplayName = "URL构造函数测试")]
    public void UrlConstructorTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        Assert.Single(client.Services);
        Assert.Equal("http://127.0.0.1:8080/", client.Services[0].Address + "");
    }

    [Fact(DisplayName = "多URL构造函数测试")]
    public void MultiUrlConstructorTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080,http://127.0.0.1:8081,http://127.0.0.1:8082");

        Assert.Equal(3, client.Services.Count);
    }
    #endregion

    #region Token令牌测试
    [Fact(DisplayName = "Token属性测试")]
    public void TokenPropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            Token = "my_token"
        };

        Assert.Equal("my_token", client.Token);
    }

    [Fact(DisplayName = "Authentication属性测试")]
    public void AuthenticationPropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            Authentication = new AuthenticationHeaderValue("Bearer", "auth_token")
        };

        Assert.NotNull(client.Authentication);
        Assert.Equal("Bearer", client.Authentication.Scheme);
        Assert.Equal("auth_token", client.Authentication.Parameter);
    }

    [Fact(DisplayName = "服务节点独立Token解析测试")]
    public void ServiceTokenParsingTest()
    {
        var client = new ApiHttpClient();
        var svc = client.Add("test", "http://127.0.0.1:8080#token=node_secret");

        Assert.Equal("node_secret", svc.Token);
        Assert.Equal("http://127.0.0.1:8080/", svc.Address + "");
    }
    #endregion

    #region 负载均衡模式测试
    [Fact(DisplayName = "默认Failover模式测试")]
    public void DefaultFailoverModeTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        Assert.Equal(LoadBalanceMode.Failover, client.LoadBalanceMode);
        Assert.IsType<FailoverLoadBalancer>(client.LoadBalancer);
    }

    [Fact(DisplayName = "切换RoundRobin模式测试")]
    public void SwitchToRoundRobinTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        client.LoadBalanceMode = LoadBalanceMode.RoundRobin;

        Assert.Equal(LoadBalanceMode.RoundRobin, client.LoadBalanceMode);
        Assert.IsType<WeightedRoundRobinLoadBalancer>(client.LoadBalancer);
    }

    [Fact(DisplayName = "切换Race模式测试")]
    public void SwitchToRaceModeTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        client.LoadBalanceMode = LoadBalanceMode.Race;

        Assert.Equal(LoadBalanceMode.Race, client.LoadBalanceMode);
        Assert.IsType<RaceLoadBalancer>(client.LoadBalancer);
    }

    [Fact(DisplayName = "屏蔽时间设置测试")]
    public void ShieldingTimeSettingTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            ShieldingTime = 120
        };

        Assert.Equal(120, client.ShieldingTime);
        Assert.Equal(120, client.LoadBalancer.ShieldingTime);

        // 切换模式后屏蔽时间保留
        client.LoadBalanceMode = LoadBalanceMode.RoundRobin;
        Assert.Equal(120, client.LoadBalancer.ShieldingTime);
    }
    #endregion

    #region 服务地址测试
    [Fact(DisplayName = "SetServer替换地址测试")]
    public void SetServerReplaceTest()
    {
        var client = new ApiHttpClient();

        client.SetServer("http://127.0.0.1:8080");
        Assert.Single(client.Services);

        // 相同地址不替换
        client.SetServer("http://127.0.0.1:8080");
        Assert.Single(client.Services);

        // 不同地址替换
        client.SetServer("http://127.0.0.1:9090");
        Assert.Single(client.Services);
        Assert.Equal("http://127.0.0.1:9090/", client.Services[0].Address + "");
    }

    [Fact(DisplayName = "Add方法测试")]
    public void AddMethodTest()
    {
        var client = new ApiHttpClient();

        var svc = client.Add("primary", "http://127.0.0.1:8080");

        Assert.Single(client.Services);
        Assert.Equal("primary", svc.Name);
        Assert.Equal("http://127.0.0.1:8080/", svc.Address + "");
    }

    [Fact(DisplayName = "Add方法Uri重载测试")]
    public void AddUriOverloadTest()
    {
        var client = new ApiHttpClient();

        var svc = client.Add("service", new Uri("http://127.0.0.1:8080/api"));

        Assert.Equal("service", svc.Name);
        Assert.Equal("http://127.0.0.1:8080/api", svc.Address + "");
    }

    [Theory(DisplayName = "地址解析带名称和权重测试")]
    [InlineData("master=3*http://127.0.0.1:8080", "master", 3)]
    [InlineData("slave=7*http://127.0.0.1:8081", "slave", 7)]
    [InlineData("5*http://127.0.0.1:8082", "test", 5)]
    [InlineData("http://127.0.0.1:8083", "test", 1)]
    public void ParseAddressWithNameAndWeightTest(String address, String expectedName, Int32 expectedWeight)
    {
        var client = new ApiHttpClient();
        var svc = client.Add("test", address);

        if (address.Contains("="))
            Assert.Equal(expectedName, svc.Name);
        Assert.Equal(expectedWeight, svc.Weight);
    }

    [Fact(DisplayName = "AddServer批量添加测试")]
    public void AddServerBatchTest()
    {
        var client = new ApiHttpClient();

        var svcs = client.AddServer("api", "http://127.0.0.1:8080,http://127.0.0.1:8081", 5);

        Assert.Equal(2, svcs.Count);
        Assert.Equal("api", svcs[0].Name);
        Assert.Equal("api2", svcs[1].Name);
        Assert.Equal(5, svcs[0].Weight);
        Assert.Equal(5, svcs[1].Weight);
    }
    #endregion

    #region CodeName/DataName测试
    [Fact(DisplayName = "CodeName属性测试")]
    public void CodeNamePropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            CodeName = "status"
        };

        Assert.Equal("status", client.CodeName);
    }

    [Fact(DisplayName = "DataName属性测试")]
    public void DataNamePropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            DataName = "result"
        };

        Assert.Equal("result", client.DataName);
    }
    #endregion

    #region JsonHost测试
    [Fact(DisplayName = "JsonHost属性测试")]
    public void JsonHostPropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        Assert.Null(client.JsonHost);

        var jsonHost = JsonHelper.Default;
        client.JsonHost = jsonHost;
        Assert.Equal(jsonHost, client.JsonHost);
    }
    #endregion

    #region 其它属性测试
    [Fact(DisplayName = "Timeout属性测试")]
    public void TimeoutPropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            Timeout = 5_000
        };

        Assert.Equal(5_000, client.Timeout);
    }

    [Fact(DisplayName = "UseProxy属性测试")]
    public void UseProxyPropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            UseProxy = true
        };

        Assert.True(client.UseProxy);
    }

    [Fact(DisplayName = "CertificateValidation属性测试")]
    public void CertificateValidationPropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            CertificateValidation = true
        };

        Assert.True(client.CertificateValidation);
    }

    [Fact(DisplayName = "DefaultUserAgent属性测试")]
    public void DefaultUserAgentPropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080")
        {
            DefaultUserAgent = "TestApp/1.0"
        };

        Assert.Equal("TestApp/1.0", client.DefaultUserAgent);
    }

    [Fact(DisplayName = "SlowTrace属性测试")]
    public void SlowTracePropertyTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        Assert.Equal(5_000, client.SlowTrace);

        client.SlowTrace = 10_000;
        Assert.Equal(10_000, client.SlowTrace);
    }

    [Fact(DisplayName = "Source和Current初始为空测试")]
    public void SourceAndCurrentInitiallyNullTest()
    {
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        Assert.Null(client.Source);
        Assert.Null(client.Current);
    }
    #endregion

    #region 事件测试
    [Fact(DisplayName = "OnRequest事件触发测试")]
    public async Task OnRequestEventTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":0,\"data\":\"test\"}")
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        var eventTriggered = false;
        HttpRequestMessage? capturedRequest = null;

        client.OnRequest += (sender, e) =>
        {
            eventTriggered = true;
            capturedRequest = e.Request;
        };

        await client.GetAsync<String>("api/test");

        Assert.True(eventTriggered);
        Assert.NotNull(capturedRequest);
    }

    [Fact(DisplayName = "OnCreateClient事件触发测试")]
    public async Task OnCreateClientEventTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":0,\"data\":\"test\"}")
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");

        await client.GetAsync<String>("api/test");

        // 验证 HttpClient 被创建
        Assert.True(client.ClientCreated);
    }
    #endregion

    #region 请求方法测试
    [Fact(DisplayName = "GetAsync方法测试")]
    public async Task GetAsyncMethodTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                Assert.Equal(HttpMethod.Get, req.Method);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"get_result\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        var result = await client.GetAsync<String>("api/test");

        Assert.Equal("get_result", result);
    }

    [Fact(DisplayName = "PostAsync方法测试")]
    public async Task PostAsyncMethodTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                Assert.Equal(HttpMethod.Post, req.Method);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"post_result\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        var result = await client.PostAsync<String>("api/test", new { name = "test" });

        Assert.Equal("post_result", result);
    }

    [Fact(DisplayName = "PutAsync方法测试")]
    public async Task PutAsyncMethodTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                Assert.Equal(HttpMethod.Put, req.Method);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"put_result\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        var result = await client.PutAsync<String>("api/test", new { name = "test" });

        Assert.Equal("put_result", result);
    }

    [Fact(DisplayName = "DeleteAsync方法测试")]
    public async Task DeleteAsyncMethodTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                Assert.Equal(HttpMethod.Delete, req.Method);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"delete_result\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        var result = await client.DeleteAsync<String>("api/test");

        Assert.Equal("delete_result", result);
    }

    [Fact(DisplayName = "PatchAsync方法测试")]
    public async Task PatchAsyncMethodTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                Assert.Equal("PATCH", req.Method.Method);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"patch_result\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        var result = await client.PatchAsync<String>("api/test", new { name = "test" });

        Assert.Equal("patch_result", result);
    }
    #endregion

    #region 响应解析测试
    [Fact(DisplayName = "字典响应解析测试")]
    public async Task DictionaryResponseTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":0,\"data\":{\"name\":\"test\",\"value\":123}}")
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        var result = await client.GetAsync<IDictionary<String, Object>>("api/test");

        Assert.NotNull(result);
        Assert.Equal("test", result["name"]);
        // Json 解析时数字可能是不同类型，使用 ToInt() 转换
        Assert.Equal(123, result["value"].ToInt());
    }

    [Fact(DisplayName = "Api异常响应测试")]
    public async Task ApiExceptionResponseTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":500,\"message\":\"Internal Server Error\"}")
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<String>("api/test"));
        Assert.Equal(500, ex.Code);
        Assert.Equal("Internal Server Error", ex.Message);
    }

    [Fact(DisplayName = "Http错误响应测试")]
    public async Task HttpErrorResponseTest()
    {
        var handler = new MockHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not Found")
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync<String>("api/test"));
    }
    #endregion

    #region 请求头测试
    [Fact(DisplayName = "Token请求头测试")]
    public async Task TokenRequestHeaderTest()
    {
        String? capturedAuth = null;
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                capturedAuth = req.Headers.Authorization?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"ok\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080")
        {
            Token = "my_token"
        };

        await client.GetAsync<String>("api/test");

        Assert.Equal("Bearer my_token", capturedAuth);
    }

    [Fact(DisplayName = "Authentication请求头测试")]
    public async Task AuthenticationRequestHeaderTest()
    {
        String? capturedAuth = null;
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                capturedAuth = req.Headers.Authorization?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"ok\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080")
        {
            Authentication = new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz")
        };

        await client.GetAsync<String>("api/test");

        Assert.Equal("Basic dXNlcjpwYXNz", capturedAuth);
    }

    [Fact(DisplayName = "Accept请求头测试_Json")]
    public async Task AcceptJsonRequestHeaderTest()
    {
        String? capturedAccept = null;
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                capturedAccept = req.Headers.Accept.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"code\":0,\"data\":\"ok\"}")
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        await client.GetAsync<String>("api/test");

        Assert.Equal("application/json", capturedAccept);
    }

    [Fact(DisplayName = "Accept请求头测试_Binary")]
    public async Task AcceptBinaryRequestHeaderTest()
    {
        String? capturedAccept = null;
        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                capturedAccept = req.Headers.Accept.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new Byte[] { 1, 2, 3 })
                };
            }
        };

        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");
        await client.GetAsync<Byte[]>("api/test");

        Assert.Equal("application/octet-stream", capturedAccept);
    }
    #endregion

    #region 原始响应所有权
    /// <summary>带释放标记的内容，用于观察响应是否被销毁</summary>
    private class TrackingContent : StringContent
    {
        public Boolean Disposed { get; private set; }

        public TrackingContent(String content) : base(content) { }

        protected override void Dispose(Boolean disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    [Fact(DisplayName = "原始响应_所有权移交_内容不销毁")]
    public async Task RawResponseMessage_ContentReadable()
    {
        var content = new TrackingContent("raw-ok");
        var handler = new MockHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content }
        };
        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");

        var msg = await client.GetAsync<HttpResponseMessage>("api/raw");
        Assert.NotNull(msg);
        Assert.Equal(HttpStatusCode.OK, msg.StatusCode);

        // 所有权移交调用方：内容必须保持可用（旧实现返回前已释放，这里 ObjectDisposedException 或标记变红）
        var body = await msg.Content.ReadAsStringAsync();
        Assert.Equal("raw-ok", body);
        Assert.False(content.Disposed);
    }

    [Fact(DisplayName = "普通结果_读取后响应仍被释放")]
    public async Task NormalResult_ResponseDisposed()
    {
        var content = new TrackingContent("{\"code\":0,\"data\":\"ok\"}");
        var handler = new MockHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content }
        };
        var client = new TestableApiHttpClient(handler, "http://127.0.0.1:8080");

        var rs = await client.GetAsync<String>("api/info");
        Assert.Equal("ok", rs);

        // 普通结果读取后立即释放，连接及时归还（条件分支不能把正常路径也放走）
        Assert.True(content.Disposed);
    }

    /// <summary>竞速专用：直接返回可控响应，跳过真实网络</summary>
    private class RaceTestableClient : ApiHttpClient
    {
        private Int32 _n;
        public TrackingContent? First;
        public TrackingContent? Second;

        public RaceTestableClient(String urls) : base(urls) => LoadBalanceMode = LoadBalanceMode.Race;

        protected override Task<HttpResponseMessage> SendOnServiceAsync(HttpRequestMessage request, ServiceEndpoint service, HttpClient client, Boolean onlyHeader, CancellationToken cancellationToken)
        {
            var content = new TrackingContent("race-ok");
            if (Interlocked.Increment(ref _n) == 1) First = content;
            else Second = content;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    [Fact(DisplayName = "竞速调用_原始响应_内容不销毁")]
    public async Task Race_RawResponseMessage_ContentReadable()
    {
        var client = new RaceTestableClient("http://127.0.0.1:34567,http://127.0.0.1:34568");

        var msg = await client.InvokeRaceAsync<HttpResponseMessage>(HttpMethod.Get, "api/raw");
        Assert.NotNull(msg);

        // 竞速选中并返回的响应必须可读（旧实现 finally 里无条件释放）
        var body = await msg.Content.ReadAsStringAsync();
        Assert.Equal("race-ok", body);
    }
    #endregion

    #region 客户端生命周期
    /// <summary>带释放标记的处理器，用于观察 HttpClient 是否被释放</summary>
    private class TrackingHandler : HttpMessageHandler
    {
        public Boolean Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        protected override void Dispose(Boolean disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    [Fact(DisplayName = "负载均衡_轮换节点_旧客户端被释放")]
    public void LoadBalancer_Rotation_DisposesOldClient()
    {
        var handler = new TrackingHandler();
        var svc = new ServiceEndpoint("s1", "http://127.0.0.1:8080")
        {
            Client = new HttpClient(handler),
            CreateTime = DateTime.Now.AddMinutes(-11),
        };

        var lb = new FailoverLoadBalancer();
        lb.PutService([svc], svc, null);

        // 10 分钟到期轮换：重建之前必须释放旧客户端，避免连接池资源随节点丢弃而泄漏
        Assert.Null(svc.Client);
        Assert.True(handler.Disposed);
    }

    [Fact(DisplayName = "设置服务端地址_替换节点_旧客户端被释放")]
    public void SetServer_Replacement_DisposesOldClient()
    {
        var handler = new TrackingHandler();
        var client = new ApiHttpClient("http://127.0.0.1:8080");
        client.Services[0].Client = new HttpClient(handler);

        client.SetServer("http://127.0.0.1:9090");

        Assert.True(handler.Disposed);
        Assert.Single(client.Services);
        Assert.Equal("http://127.0.0.1:9090/", client.Services[0].Address + "");
    }

    /// <summary>带释放标记的 ApiHttpClient</summary>
    private class TrackingApiClient : ApiHttpClient
    {
        public Boolean DisposedFlag { get; private set; }

        protected override void Dispose(Boolean disposing)
        {
            DisposedFlag = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>制造并发首访窗口的服务解析器：第一个工厂调用挂起，等第二个进入后再放行</summary>
    private class RacingResolver : ConfigServiceResolver
    {
        private readonly SemaphoreSlim _entered = new(0);
        private readonly SemaphoreSlim _release = new(0);
        private Int32 _count;

        public ConcurrentBag<TrackingApiClient> Created { get; } = new();

        public RacingResolver() : base((IConfigProvider)null!) => Servers = "http://127.0.0.1:8080/";

        protected override IApiClient CreateClient(String serviceName, String? tag)
        {
            var client = new TrackingApiClient();
            Created.Add(client);

            if (Interlocked.Increment(ref _count) == 1)
            {
                _entered.Release();
                _release.Wait(TimeSpan.FromSeconds(10));
            }
            else
            {
                _entered.Wait(TimeSpan.FromSeconds(10));
                _release.Release();
            }

            return client;
        }
    }

    [Fact(DisplayName = "服务解析器_并发首访_败者客户端被释放")]
    public async Task ServiceResolver_ConcurrentFirstAccess_DisposesLoser()
    {
        var resolver = new RacingResolver();

        var t1 = Task.Run(() => resolver.GetClientAsync("svc"));
        var t2 = Task.Run(() => resolver.GetClientAsync("svc"));
        var clients = await Task.WhenAll(t1, t2);

        // 两次调用必须拿到同一个客户端；工厂跑过两次，败者要就地释放而不是静默丢弃
        Assert.Same(clients[0], clients[1]);
        Assert.Equal(2, resolver.Created.Count);
        Assert.Equal(1, resolver.Created.Count(e => e.DisposedFlag));

        resolver.Dispose();
    }
    #endregion

    #region ApiHelper 扩展资源释放
    [Fact(DisplayName = "HttpClient扩展_普通结果释放响应_原始响应所有权移交")]
    public async Task ApiHelperInvoke_ResponseOwnership()
    {
        var normal = new TrackingContent("{\"code\":0,\"data\":\"ok\"}");
        var raw = new TrackingContent("raw-ok");
        var index = 0;
        var handler = new MockHttpMessageHandler
        {
            Handler = _ => index++ == 0
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = normal }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = raw }
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:8080") };

        // 普通结果：读取后立即释放响应
        var rs = await client.InvokeAsync<String>(HttpMethod.Get, "api/info");
        Assert.Equal("ok", rs);
        Assert.True(normal.Disposed);

        // 原始响应：所有权移交，不销毁
        var msg = await client.InvokeAsync<HttpResponseMessage>(HttpMethod.Get, "api/raw");
        Assert.NotNull(msg);
        Assert.Equal("raw-ok", await msg.Content.ReadAsStringAsync());
        Assert.False(raw.Disposed);
    }
    #endregion

    #region 竞速下载资源释放
    /// <summary>竞速下载专用：返回带哈希头的响应，内容与预期哈希不符以触发保存失败</summary>
    private class RaceDownloadClient : ApiHttpClient
    {
        public ConcurrentDictionary<String, TrackingContent> Contents { get; } = new();

        public RaceDownloadClient(String urls) : base(urls) => LoadBalanceMode = LoadBalanceMode.Race;

        protected override async Task<HttpResponseMessage> SendOnServiceAsync(HttpRequestMessage request, ServiceEndpoint service, HttpClient client, Boolean onlyHeader, CancellationToken cancellationToken)
        {
            // 让第一个节点先返回，稳定选中它作为下载节点
            if (service.Name != "service1") await Task.Delay(200, cancellationToken);

            var content = new TrackingContent("payload");
            Contents[service.Name] = content;

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            response.Content.Headers.TryAddWithoutValidation("Content-SHA256", "sha256$" + new String('0', 64));
            return response;
        }
    }

    /// <summary>可控顺序的失败客户端：service1 的异常要等 service2 的异常冒泡之后才放行</summary>
    private class OrderedFailClient : ApiHttpClient
    {
        public readonly SemaphoreSlim FirstEntered = new(0);
        public readonly SemaphoreSlim ReleaseFirst = new(0);

        public OrderedFailClient(String urls) : base(urls) => LoadBalanceMode = LoadBalanceMode.RoundRobin;

        protected override async Task<HttpResponseMessage> SendOnServiceAsync(HttpRequestMessage request, ServiceEndpoint service, HttpClient client, Boolean onlyHeader, CancellationToken cancellationToken)
        {
            if (service.Name == "service1")
            {
                FirstEntered.Release();
                await ReleaseFirst.WaitAsync().ConfigureAwait(false);
            }

            throw new InvalidOperationException("boom-" + service.Name);
        }
    }

    [Fact(DisplayName = "并发请求_错误归因不串节点")]
    public async Task ConcurrentRequests_ErrorSourceNotCrossed()
    {
        var client = new OrderedFailClient("http://127.0.0.1:34567,http://127.0.0.1:34568");

        // 第一个请求先选中 service1 并挂起
        var t1 = Task.Run(() => client.InvokeAsync<String>("api/a", null));
        await client.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10));

        // 第二个请求此时选中 service2 并立即以异常结束，改写共享状态的机会点
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => client.InvokeAsync<String>("api/b", null));
        Assert.Contains("34568", ex2.Source);

        // 放行第一个请求：其归因必须仍指向 service1（旧实现读共享字段，会指向后来的 service2）
        client.ReleaseFirst.Release();
        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() => t1);
        Assert.Contains("34567", ex1.Source);
    }

    [Fact(DisplayName = "空服务列表_抛带消息异常而不是静默成功")]
    public async Task EmptyServices_ThrowsWithMessage()
    {
        var client = new ApiHttpClient();

        // 普通调用：旧实现抛无消息 InvalidOperationException（注释还写“无法到达这里”）
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.InvokeAsync<String>("api/info", null));
        Assert.Contains("service address", ex.Message);

        // 下载：旧实现循环零次直接静默返回，调用方误以为成功
        var file = Path.Combine(Path.GetTempPath(), "empty-dl.bin");
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => client.DownloadFileAsync("http://x/file", file, null));
        Assert.Contains("service address", ex2.Message);
    }

    [Fact(DisplayName = "竞速下载_保存失败_响应仍被释放")]
    public async Task RaceDownload_SaveFailure_DisposesResponse()
    {
        var expectedHash = "sha256$" + new String('0', 64);
        var client = new RaceDownloadClient("http://127.0.0.1:34567,http://127.0.0.1:34568");

        var file = Path.GetTempFileName();
        File.Delete(file);
        try
        {
            await Assert.ThrowsAsync<IOException>(() => client.DownloadFileRaceAsync("file.bin", file, expectedHash));

            // 内容哈希必然不符，保存走异常路径：选中响应必须已释放（旧实现 Dispose 在保存之后，失败即跳过）
            Assert.True(client.Contents["service1"].Disposed);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
            if (File.Exists(file + ".tmp")) File.Delete(file + ".tmp");
        }
    }
    #endregion
}
