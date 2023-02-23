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
            else
                rs.Content = request.Content != null ? request.Content : new StringContent(request.Headers.ToString());

            return rs;
        }
    }

    [Fact]
    public async void PostJson()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient();
        var json = client.PostJson(url, new { state = "1234" });
        Assert.NotNull(json);
        Assert.Contains("\"name\":\"StarWeb\"", json);

        json = await client.PostJsonAsync(url, new { state = "abcd" });
        Assert.NotNull(json);
        Assert.Contains("\"name\":\"StarWeb\"", json);
    }

    [Fact]
    public async void PostXml()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient();
        var json = client.PostXml(url, new { state = "1234" });
        Assert.NotNull(json);
        Assert.Contains("\"name\":\"StarWeb\"", json);

        json = await client.PostXmlAsync(url, new { state = "abcd" });
        Assert.NotNull(json);
        Assert.Contains("\"name\":\"StarWeb\"", json);
    }

    [Fact]
    public async void PostForm()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient();
        var json = client.PostForm(url, new { state = "1234" });
        Assert.NotNull(json);
        Assert.Contains("\"name\":\"StarWeb\"", json);

        json = await client.PostFormAsync(url, new { state = "abcd" });
        Assert.NotNull(json);
        Assert.Contains("\"name\":\"StarWeb\"", json);
    }

    [Fact]
    public void GetString()
    {
        var url = "http://star.newlifex.com/cube/info";

        var client = new HttpClient();
        var json = client.GetString(url, new Dictionary<String, String> { { "state", "xxxyyy" } });

        Assert.NotNull(json);
        Assert.Contains("\"name\":\"StarWeb\"", json);
    }
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
public async void UploadFile()
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
}