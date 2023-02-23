using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using NewLife.Http;
using NewLife.Log;
using NewLife.Serialization;
using Xunit;
using Moq;

namespace XUnitTest.Http;

public class HttpHelperTests
{
    static HttpHelperTests() => HttpHelper.Tracer = new DefaultTracer();

    class MyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //var body = await request.Content.ReadAsByteArrayAsync();

            var rs = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = request.Content
            };

            if (request.Content == null)
            {               
                rs.Content = new StringContent(request.Headers.ToString());
            }

            return Task.FromResult(rs);
        }
    }

    [Fact]
    public async void PostJson()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient(new MyHandler());
        client.SetUserAgent();

        var rs = client.PostJson(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Equal("""{"state":"1234","state2":"abcd"}""", rs);

        rs = await client.PostJsonAsync(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Equal("""{"state":"1234","state2":"abcd"}""", rs);
    }

    [Fact]
    public async void PostXml()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient(new MyHandler());
        var rs = client.PostXml(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Contains("""
            <_f__AnonymousType0_2>
              <state>1234</state>
              <state2>abcd</state2>
            </_f__AnonymousType0_2>
            """, rs);

        rs = await client.PostXmlAsync(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Contains("""
            <_f__AnonymousType0_2>
              <state>1234</state>
              <state2>abcd</state2>
            </_f__AnonymousType0_2>
            """, rs);
    }

    [Fact]
    public async void PostForm()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient(new MyHandler());
        var rs = client.PostForm(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Contains("state=1234&state2=abcd", rs);

        rs = await client.PostFormAsync(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Contains("state=1234&state2=abcd", rs);
    }

    [Fact]
    public void GetString()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient(new MyHandler());
        var rs = client.GetString(url, new Dictionary<String, String> { { "state", "xxxyyy" } });

        Assert.NotNull(rs);
        Assert.Equal("state: xxxyyy\r\n", rs);
    }
}