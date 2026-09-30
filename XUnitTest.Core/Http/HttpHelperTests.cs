using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
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

    [Fact(DisplayName = "MakeResponse_无实体_补Content-Length为零")]
    public void MakeResponse_NoBody_WritesZeroContentLength()
    {
        // 不声明长度时报文不可自定界，keep-alive 下客户端会把下一个响应当成本次实体
        var pk = HttpHelper.MakeResponse(HttpStatusCode.OK, null, null);
        var text = pk.ToStr();

        Assert.Contains("Content-Length: 0\r\n", text);
        Assert.EndsWith("\r\n\r\n", text);
    }

    [Fact(DisplayName = "MakeResponse_有实体_写明长度并接上负载")]
    public void MakeResponse_WithBody_WritesLength()
    {
        var pk = HttpHelper.MakeResponse(HttpStatusCode.OK, null, new ArrayPacket("hello".GetBytes()));
        var text = pk.ToStr();

        Assert.Contains("Content-Length: 5\r\n", text);
        Assert.EndsWith("hello", text);
    }

    [Theory(DisplayName = "MakeResponse_无实体状态码_不声明Content-Length")]
    [InlineData(HttpStatusCode.Continue)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotModified)]
    public void MakeResponse_NoEntityStatus_OmitsContentLength(HttpStatusCode code)
    {
        var pk = HttpHelper.MakeResponse(code, null, null);

        // RFC 7230 §3.3.2：1xx/204/304 不得携带 Content-Length
        Assert.DoesNotContain("Content-Length", pk.ToStr());
    }

    [Fact(DisplayName = "MakeResponse_调用方已声明长度_不重复写入")]
    public void MakeResponse_ExistingContentLength_NotDuplicated()
    {
        // 重复的 Content-Length 是 CL.CL 走私面，宁可少补也不能补出第二个
        var headers = new Dictionary<String, Object?> { ["Content-Length"] = "0" };
        var pk = HttpHelper.MakeResponse(HttpStatusCode.OK, headers, null);
        var text = pk.ToStr();

        Assert.Equal(1, text.Split("Content-Length").Length - 1);
    }

    [Fact(DisplayName = "MakeResponse_已声明分块传输_不补Content-Length")]
    public void MakeResponse_TransferEncodingHeader_NoContentLength()
    {
        var headers = new Dictionary<String, Object?> { ["Transfer-Encoding"] = "chunked" };
        var pk = HttpHelper.MakeResponse(HttpStatusCode.OK, headers, null);
        var text = pk.ToStr();

        Assert.DoesNotContain("Content-Length", text);
        Assert.Contains("Transfer-Encoding: chunked", text);
    }
}