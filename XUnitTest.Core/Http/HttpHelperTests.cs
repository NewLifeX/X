using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Http;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Http;

public class HttpHelperTests
{
    static HttpHelperTests() => HttpHelper.Tracer = new DefaultTracer();

    class MyHandler : HttpMessageHandler
    {
        public Byte[] Data { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //var body = await request.Content.ReadAsByteArrayAsync();

            var rs = new HttpResponseMessage(HttpStatusCode.OK);

            if (Data != null)
                rs.Content = new ByteArrayContent(Data);
            else rs.Content = request.Content != null ? request.Content : new StringContent(request.Headers.ToString());

            return Task.FromResult(rs);
        }
    }

    [Fact]
    public async Task PostJson()
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
    public async Task PostXml()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient(new MyHandler());
        var rs = client.PostXml(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Contains("""
            <_f__AnonymousType4_2>
              <state>1234</state>
              <state2>abcd</state2>
            </_f__AnonymousType4_2>
            """, rs);

        rs = await client.PostXmlAsync(url, new { state = "1234", state2 = "abcd" });
        Assert.NotNull(rs);
        Assert.Contains("""
            <_f__AnonymousType4_2>
              <state>1234</state>
              <state2>abcd</state2>
            </_f__AnonymousType4_2>
            """, rs);
    }

    [Fact]
    public async Task PostForm()
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

    [Fact]
    public async Task DownloadFile()
    {
        var url = "http://star.newlifex.com/cube/info";
        var file = "down.txt";
        var file2 = file.GetFullPath();

        if (File.Exists(file2)) File.Delete(file2);

        var txt = "学无先后达者为师！";
        var sb = new StringBuilder();
        for (var i = 0; i < 1000; i++)
        {
            sb.AppendLine(txt);
        }

        var client = new HttpClient(new MyHandler { Data = sb.ToString().GetBytes() });
        await client.DownloadFileAsync(url, file);

        Assert.True(File.Exists(file2));
    }

    [Fact]
    public async Task UploadFile()
    {
        var url = "http://star.newlifex.com/cube/info";
        var file = "up.txt";
        var file2 = file.GetFullPath();

        var txt = "学无先后达者为师！";
        File.WriteAllText(file2, txt);

        var client = new HttpClient(new MyHandler());
        var rs = await client.UploadFileAsync(url, file, new { state = "1234", state2 = "abcd" });

        Assert.NotNull(rs);
        //Assert.Equal("""
        //    --05331b02-8d38-4905-902f-119335443546
        //    Content-Disposition: form-data; name=file; filename=up.txt; filename*=utf-8''up.txt

        //    学无先后达者为师！
        //    --05331b02-8d38-4905-902f-119335443546
        //    Content-Type: text/plain; charset=utf-8
        //    Content-Disposition: form-data; name=state

        //    1234
        //    --05331b02-8d38-4905-902f-119335443546
        //    Content-Type: text/plain; charset=utf-8
        //    Content-Disposition: form-data; name=state2

        //    abcd
        //    --05331b02-8d38-4905-902f-119335443546--

        //    """, rs);
        Assert.Contains("Content-Disposition: form-data; name=file; filename=up.txt; filename*=utf-8''up.txt", rs);
        Assert.Contains(txt, rs);
        Assert.Contains("Content-Type: text/plain; charset=utf-8", rs);
        Assert.Contains("Content-Disposition: form-data; name=state", rs);
        Assert.Contains("1234", rs);
        Assert.Contains("Content-Disposition: form-data; name=state2", rs);
        Assert.Contains("abcd", rs);
    }

    [Fact]
    public void GetStringWithDnsResolver()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = DefaultTracer.Instance.CreateHttpClient();
        var rs = client.GetString(url, new Dictionary<String, String> { { "state", "xxxyyy" } });

        Assert.NotNull(rs);
        //Assert.Equal("state: xxxyyy\r\n", rs);
        Assert.Contains("\"name\":\"StarWeb\"", rs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetStringWithDnsResolver2()
    {
        var url = "https://sso.newlifex.com/cube/info";

        var client = DefaultTracer.Instance.CreateHttpClient();
        var rs = client.GetString(url, new Dictionary<String, String> { { "state", "xxxyyy" } });

        Assert.NotNull(rs);
        //Assert.Equal("state: xxxyyy\r\n", rs);
        Assert.Contains("\"name\":\"CubeSSO\"", rs);
    }
}