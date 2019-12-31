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
    public class ApiHelperTest : DisposeBase
    {
        private readonly ApiServer _Server;
        private HttpClient _Client;

        public ApiHelperTest()
        {
            _Server = new ApiServer(12346)
            {
                Log = XTrace.Log,
            };
            _Server.Start();

            _Client = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1:12346")
            };
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _Server.TryDispose();
        }

        [Fact(DisplayName = "异步请求")]
        public async void SendAsyncTest()
        {
            var dic = await _Client.GetAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(dic);
            Assert.True(dic.Count > 10);
            Assert.StartsWith("testhost", (dic["Server"] + ""));

            var pk = await _Client.GetAsync<Packet>("api/info");
            Assert.NotNull(pk);
            Assert.True(pk.Total > 100);

            var ss = await _Client.PostAsync<String[]>("Api/All");
            Assert.NotNull(ss);
            Assert.True(ss.Length >= 3);
        }

        [Fact(DisplayName = "异常请求")]
        public async void ErrorTest()
        {
            var msg = await _Client.GetAsync<HttpResponseMessage>("api/info");
            Assert.NotNull(msg);
            Assert.Equal(HttpStatusCode.OK, msg.StatusCode);

            //msg = await _Client.InvokeAsync<HttpResponseMessage>("api/info3");
            //Assert.NotNull(msg);
            //Assert.Equal(HttpStatusCode.NotFound, msg.StatusCode);

            //var str = await msg.Content.ReadAsStringAsync();
            //Assert.Equal("\"无法找到名为[api/info3]的服务！\"", str);

            try
            {
                var dic = await _Client.GetAsync<Object>("api/info3");
            }
            catch (ApiException ex)
            {
                Assert.Equal(404, ex.Code);
                Assert.Equal("无法找到名为[api/info3]的服务！", ex.Message);
                //Assert.Equal(_Client.BaseAddress + "api/info3", ex.Source);
            }
        }

        [Fact(DisplayName = "上传数据")]
        public async void PostAsyncTest()
        {
            var state = Rand.NextString(8);
            var state2 = Rand.NextString(8);
            var dic = await _Client.GetAsync<IDictionary<String, Object>>("api/info", new { state, state2 });
            Assert.NotNull(dic);
            Assert.Equal(state, dic[nameof(state)]);
            Assert.NotEqual(state2, dic[nameof(state2)]);

            var msg = await _Client.GetAsync<HttpResponseMessage>("api/info", new { state, state2 });
            Assert.NotNull(msg);
            Assert.Equal(HttpMethod.Get, msg.RequestMessage.Method);

            state = Rand.NextString(1000 + 8);
            msg = await _Client.PostAsync<HttpResponseMessage>("api/info", new { state, state2 });
            Assert.NotNull(msg);
            Assert.Equal(HttpMethod.Post, msg.RequestMessage.Method);
        }
    }
}