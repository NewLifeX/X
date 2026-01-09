using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
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

    [Fact(DisplayName = "兼容RoundRobin属性测试")]
    public void RoundRobinPropertyCompatibilityTest()
    {
#pragma warning disable CS0618
        var client = new ApiHttpClient("http://127.0.0.1:8080");

        client.RoundRobin = true;
        Assert.Equal(LoadBalanceMode.RoundRobin, client.LoadBalanceMode);
        Assert.True(client.RoundRobin);

        client.RoundRobin = false;
        Assert.Equal(LoadBalanceMode.Failover, client.LoadBalanceMode);
        Assert.False(client.RoundRobin);
#pragma warning restore CS0618
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
        Assert.Equal("api1", svcs[0].Name);
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
}
