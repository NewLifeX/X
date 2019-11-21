using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Remoting
{
    public class ApiTest : DisposeBase
    {
        private readonly ApiServer _Server;

        public ApiTest()
        {
            _Server = new ApiServer(12345)
            {
                Log = XTrace.Log,
                //EncoderLog = XTrace.Log,
            };
            _Server.Handler = new TokenApiHandler { Host = _Server };
            _Server.Start();
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _Server.TryDispose();
        }

        [Fact(DisplayName = "基础Api测试")]
        public async void BasicTest()
        {
            var client = new ApiClient("tcp://127.0.0.1:12345")
            {
                Log = XTrace.Log
            };
            //client.EncoderLog = XTrace.Log;

            var apis = await client.InvokeAsync<String[]>("api/all");
            Assert.NotNull(apis);
            Assert.Equal(3, apis.Length);
            Assert.Equal("String[] Api/All()", apis[0]);
            Assert.Equal("Object Api/Info(String state)", apis[1]);
            Assert.Equal("Packet Api/Info2(Packet state)", apis[2]);
        }

        [Theory(DisplayName = "参数测试")]
        [InlineData("12345678", "ABCDEFG")]
        [InlineData("ABCDEFG", "12345678")]
        public async void InfoTest(String state, String state2)
        {
            var client = new ApiClient("tcp://127.0.0.1:12345")
            {
                Log = XTrace.Log
            };

            //var state = Rand.NextString(8);
            //var state2 = Rand.NextString(8);

            var infs = await client.InvokeAsync<IDictionary<String, Object>>("api/info", new { state, state2 });
            Assert.NotNull(infs);
            Assert.Equal(Environment.MachineName, infs["MachineName"]);
            Assert.Equal(Environment.UserName, infs["UserName"]);

            Assert.Equal(state, infs["state"]);
            Assert.Null(infs["state2"]);
        }

        [Fact(DisplayName = "二进制测试")]
        public async void Info2Test()
        {
            var client = new ApiClient("tcp://127.0.0.1:12345")
            {
                Log = XTrace.Log
            };

            var buf = Rand.NextBytes(32);

            var pk = await client.InvokeAsync<Packet>("api/info2", buf);
            Assert.NotNull(pk);
            Assert.True(pk.Total > buf.Length);
            Assert.Equal(buf, pk.Slice(pk.Total - buf.Length, -1).ToArray());
        }

        [Fact(DisplayName = "异常请求")]
        public async void ErrorTest()
        {
            var client = new ApiClient("tcp://127.0.0.1:12345")
            {
                Log = XTrace.Log
            };

            try
            {
                var msg = await client.InvokeAsync<Object>("api/info3");
            }
            catch (Exception ex)
            {
                var aex = ex as ApiException;
                Assert.NotNull(aex);
                Assert.Equal(404, aex.Code);
                Assert.Equal("远程[Api]错误！ \"无法找到名为[api/info3]的服务！\"", ex.Message);
            }
        }

        [Theory(DisplayName = "令牌测试")]
        [InlineData("12345678", "ABCDEFG")]
        [InlineData("ABCDEFG", "12345678")]
        public async void TokenTest(String token, String state)
        {
            var client = new ApiClient("tcp://127.0.0.1:12345")
            {
                Log = XTrace.Log,
                Token = token,
            };

            var infs = await client.InvokeAsync<IDictionary<String, Object>>("api/info", new { state });
            Assert.NotNull(infs);
            Assert.Equal(token, infs["token"]);

            // 另一个客户端，共用令牌，应该可以拿到上一次状态数据
            var client2 = new ApiClient("tcp://127.0.0.1:12345")
            {
                Log = XTrace.Log,
                Token = token,
            };

            infs = await client2.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
            Assert.Equal(state, infs["LastState"]);
        }
    }
}