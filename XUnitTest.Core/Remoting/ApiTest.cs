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
            var client = new ApiClient("tcp://127.0.0.1:12345");
            client.Log = XTrace.Log;
            //client.EncoderLog = XTrace.Log;

            var apis = await client.InvokeAsync<String[]>("api/all");
            Assert.NotNull(apis);
            Assert.Equal(3, apis.Length);
            Assert.Equal("String[] Api/All()", apis[0]);
            Assert.Equal("Object Api/Info(String state)", apis[1]);
            Assert.Equal("Packet Api/Info2(Packet state)", apis[2]);
        }

        [Fact(DisplayName = "参数测试")]
        public async void InfoTest()
        {
            var client = new ApiClient("tcp://127.0.0.1:12345");
            client.Log = XTrace.Log;

            var state = Rand.NextString(8);
            var state2 = Rand.NextString(8);

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
            var client = new ApiClient("tcp://127.0.0.1:12345");
            client.Log = XTrace.Log;

            var buf = Rand.NextBytes(32);

            var pk = await client.InvokeAsync<Packet>("api/info2", buf);
            Assert.NotNull(pk);
            Assert.True(pk.Total > buf.Length);
            Assert.Equal(buf, pk.Slice(pk.Total - buf.Length, -1).ToArray());
        }
    }
}