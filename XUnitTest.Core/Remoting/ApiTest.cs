using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;
using NewLife.Security;
using Xunit;
//using Xunit.Extensions.Ordering;

namespace XUnitTest.Remoting
{
    public class ApiTest : DisposeBase
    {
        private readonly ApiServer _Server;
        private readonly ApiClient _Client;
        private String _Uri;

        public ApiTest()
        {
            var port = Rand.Next(10000, 65535);

            _Server = new ApiServer(port)
            {
                Log = XTrace.Log,
                //EncoderLog = XTrace.Log,
                ShowError = true,
            };
            _Server.Handler = new TokenApiHandler { Host = _Server };
            _Server.Start();

            _Uri = $"tcp://127.0.0.1:{port}";

            var client = new ApiClient(_Uri)
            {
                //Log = XTrace.Log
            };
            //client.EncoderLog = XTrace.Log;
            _Client = client;
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _Server.TryDispose();
        }

        //[Order(1)]
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

        //[Order(2)]
        [Theory(DisplayName = "参数测试")]
        [InlineData("12345678", "ABCDEFG")]
        [InlineData("ABCDEFG", "12345678")]
        public async void InfoTest(String state, String state2)
        {
            var infs = await _Client.InvokeAsync<IDictionary<String, Object>>("api/info", new { state, state2 });
            Assert.NotNull(infs);
            Assert.Equal(Environment.MachineName, infs["MachineName"]);
            Assert.Equal(Environment.UserName, infs["UserName"]);

            Assert.Equal(state, infs["state"]);
            Assert.Null(infs["state2"]);
        }

        //[Order(3)]
        [Fact(DisplayName = "二进制测试")]
        public async void Info2Test()
        {
            var buf = Rand.NextBytes(32);

            var pk = await _Client.InvokeAsync<Packet>("api/info2", buf);
            Assert.NotNull(pk);
            Assert.True(pk.Total > buf.Length);
            Assert.Equal(buf, pk.Slice(pk.Total - buf.Length, -1).ToArray());
        }

        //[Order(4)]
        [Fact(DisplayName = "异常请求")]
        public async void ErrorTest()
        {
            var ex = await Assert.ThrowsAsync<ApiException>(() => _Client.InvokeAsync<Object>("api/info3"));

            Assert.NotNull(ex);
            Assert.Equal(404, ex.Code);
            Assert.Equal("无法找到名为[api/info3]的服务！", ex.Message);

            var uri = new NetUri(_Client.Servers[0]);
            Assert.Equal(uri + "/api/info3", ex.Source);
        }

        [Theory(DisplayName = "令牌测试")]
        [InlineData("12345678", "ABCDEFG")]
        [InlineData("ABCDEFG", "12345678")]
        public async void TokenTest(String token, String state)
        {
            var client = new ApiClient(_Uri)
            {
                //Log = XTrace.Log,
                Token = token,
            };

            var infs = await client.InvokeAsync<IDictionary<String, Object>>("api/info", new { state });
            Assert.NotNull(infs);
            Assert.Equal(token, infs["token"]);

            // 另一个客户端，共用令牌，应该可以拿到上一次状态数据
            var client2 = new ApiClient(_Uri)
            {
                //Log = XTrace.Log,
                Token = token,
            };

            infs = await client2.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
            Assert.Equal(state, infs["LastState"]);
        }
    }
}