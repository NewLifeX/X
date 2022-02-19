using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using NewLife.Http;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Http
{
    public class HttpHelperTests
    {
        static HttpHelperTests()
        {
            HttpHelper.Tracer = new DefaultTracer();
        }

        [Fact]
        public async void PostJson()
        {
            var url = "http://star.newlifex.com/cube/info";

            var client = new HttpClient();

            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var asmName = asm?.GetName();
            if (asmName != null)
            {
                //var userAgent = $"{asmName.Name}/{asmName.Version}({Environment.OSVersion};{Environment.Version})";
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(asmName.Name, asmName.Version + ""));
            }

            var json = client.PostJson(url, new { state = "1234" });
            Assert.NotNull(json);
            Assert.Contains("\"server\":\"StarWeb\"", json);

            json = await client.PostJsonAsync(url, new { state = "abcd" });
            Assert.NotNull(json);
            Assert.Contains("\"server\":\"StarWeb\"", json);
        }

        [Fact]
        public async void PostXml()
        {
            var url = "http://star.newlifex.com/cube/info";

            var client = new HttpClient();
            var json = client.PostXml(url, new { state = "1234" });
            Assert.NotNull(json);
            Assert.Contains("\"server\":\"StarWeb\"", json);

            json = await client.PostXmlAsync(url, new { state = "abcd" });
            Assert.NotNull(json);
            Assert.Contains("\"server\":\"StarWeb\"", json);
        }

        [Fact]
        public async void PostForm()
        {
            var url = "http://star.newlifex.com/cube/info";

            var client = new HttpClient();
            var json = client.PostForm(url, new { state = "1234" });
            Assert.NotNull(json);
            Assert.Contains("\"server\":\"StarWeb\"", json);

            json = await client.PostFormAsync(url, new { state = "abcd" });
            Assert.NotNull(json);
            Assert.Contains("\"server\":\"StarWeb\"", json);
        }

        [Fact]
        public void GetString()
        {
            var url = "http://star.newlifex.com/cube/info";

            var client = new HttpClient();
            var json = client.GetString(url, new Dictionary<String, String> { { "state", "xxxyyy" } });

            Assert.NotNull(json);
            Assert.Contains("\"server\":\"StarWeb\"", json);
        }
    }
}