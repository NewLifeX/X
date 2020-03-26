using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Core
{
    public class TinyHttpClientTest
    {
        private TinyHttpClient _Client;

        public TinyHttpClientTest()
        {
            _Client = new TinyHttpClient();
        }

        [Fact(DisplayName = "同步请求")]
        public void SendTest()
        {
            var uri = new Uri("http://www.newlifex.com");
            var client = new TinyHttpClient();
            var html = client.Send(uri, null)?.ToStr();

            Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
            Assert.Equal(uri, client.BaseAddress);
        }

        [Fact(DisplayName = "异步请求")]
        public async void SendAsyncTest()
        {
            var uri = new Uri("http://www.newlifex.com");
            var client = new TinyHttpClient();
            var html = (await client.SendAsync(uri, null))?.ToStr();

            Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
            Assert.Equal(uri, client.BaseAddress);
        }

        [Fact(DisplayName = "同步字符串")]
        public void GetString()
        {
            var url = "http://x.newlifex.com";
            var client = new TinyHttpClient();
            var html = client.GetString(url);

            Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
        }

        [Fact(DisplayName = "异步字符串")]
        public async void GetStringAsync()
        {
            var url = "http://x.newlifex.com";
            var client = new TinyHttpClient();
            var html = await client.GetStringAsync(url);

            Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
        }

        [Fact(DisplayName = "同步https")]
        public void GetStringHttps()
        {
            var url = "https://x.newlifex.com";
            var client = new TinyHttpClient();
            var html = client.GetString(url);

            Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
        }
    }
}
