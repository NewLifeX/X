using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Xunit;
using NewLife.Remoting;
using System.Net;
using NewLife.Data;

namespace XUnitTest.Remoting
{
    public class ApiHelperTest
    {
        private HttpClient _Client;

        public ApiHelperTest()
        {
            _Client = new HttpClient
            {
                BaseAddress = new Uri("http://feifan.link:2233")
            };
        }

        [Fact(DisplayName = "同步请求")]
        public void SendTest()
        {
            var html = _Client.Invoke<String>("api/info");

            Assert.True(!html.IsNullOrEmpty() && html.Length > 500);

            var dic = _Client.Invoke<IDictionary<String, Object>>("api/info");
            Assert.NotNull(dic);
            Assert.True(dic.Count > 10);
            Assert.Equal("xLinkServer", dic["Server"] + "");
        }

        [Fact(DisplayName = "异步请求")]
        public async void SendAsyncTest()
        {
            var html = await _Client.InvokeAsync<String>("api/info");

            Assert.True(!html.IsNullOrEmpty() && html.Length > 500);

            var dic = await _Client.InvokeAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(dic);
            Assert.True(dic.Count > 10);
            Assert.Equal("xLinkServer", dic["Server"] + "");

            var pk = await _Client.InvokeAsync<Packet>("api/info");
            Assert.NotNull(pk);
            Assert.True(pk.Total > 100);
        }

        [Fact(DisplayName = "异常请求")]
        public async void ErrorTest()
        {
            var msg = await _Client.InvokeAsync<HttpResponseMessage>("api/info");
            Assert.NotNull(msg);
            Assert.Equal(HttpStatusCode.OK, msg.StatusCode);

            msg = await _Client.InvokeAsync<HttpResponseMessage>("api/info3");
            Assert.NotNull(msg);
            Assert.Equal(HttpStatusCode.NotFound, msg.StatusCode);

            var str = await msg.Content.ReadAsStringAsync();
            Assert.Equal("\"无法找到名为[api/info3]的服务！\"", str);

            try
            {
                var dic = await _Client.InvokeAsync<Object>("api/info3");
            }
            catch (ApiException ex)
            {
                Assert.Equal(404, ex.Code);
                Assert.Equal("无法找到名为[api/info3]的服务！", ex.Message);
            }
        }
    }
}