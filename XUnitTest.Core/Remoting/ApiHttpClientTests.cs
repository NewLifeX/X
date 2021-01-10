using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Remoting
{
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

        [Fact(DisplayName = "基础Api测试")]
        public async void BasicTest()
        {
            var apis = await _Client.InvokeAsync<String[]>("api/all");
            Assert.NotNull(apis);
            Assert.Equal(3, apis.Length);
            Assert.Equal("String[] Api/All()", apis[0]);
            Assert.Equal("Object Api/Info(String state)", apis[1]);
            Assert.Equal("Packet Api/Info2(Packet state)", apis[2]);
        }

        [Fact(DisplayName = "参数测试")]
        public async void InfoTest()
        {
            var state = Rand.NextString(8);
            var state2 = Rand.NextString(8);

            var infs = await _Client.InvokeAsync<IDictionary<String, Object>>("api/info", new { state, state2 });
            Assert.NotNull(infs);
            Assert.Equal(Environment.MachineName, infs["MachineName"]);
            Assert.Equal(Environment.UserName, infs["UserName"]);

            Assert.Equal(state, infs["state"]);
            Assert.Null(infs["state2"]);
        }

        [Fact(DisplayName = "二进制测试")]
        public async void Info2Test()
        {
            var buf = Rand.NextBytes(32);

            var pk = await _Client.InvokeAsync<Packet>("api/info2", buf);
            Assert.NotNull(pk);
            Assert.True(pk.Total > buf.Length);
            Assert.Equal(buf, pk.Slice(pk.Total - buf.Length, -1).ToArray());
        }

        [Fact(DisplayName = "异常请求")]
        public async void ErrorTest()
        {
            var ex = await Assert.ThrowsAsync<ApiException>(() => _Client.InvokeAsync<Object>("api/info3"));

            Assert.NotNull(ex);
            Assert.Equal(404, ex.Code);
            //Assert.True(ex.Message.EndsWith("无法找到名为[api/info3]的服务！"));
            Assert.EndsWith("无法找到名为[api/info3]的服务！", ex.Message);
        }

        [Theory(DisplayName = "令牌测试")]
        [InlineData("12345678", "ABCDEFG")]
        [InlineData("ABCDEFG", "12345678")]
        public async void TokenTest(String token, String state)
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
            Assert.Equal(state, infs["LastState"]);
        }

        [Fact]
        public async void SlaveTest()
        {
            var client = new ApiHttpClient("http://127.0.0.1:10000,http://127.0.0.1:20000," + _Address)
            {
                Timeout = 3_000
            };
            var ac = client as IApiClient;

            var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
        }
    }
}