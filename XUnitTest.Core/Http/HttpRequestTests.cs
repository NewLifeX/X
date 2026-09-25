using NewLife;
using NewLife.Data;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HttpRequest 解析与构建测试</summary>
public class HttpRequestTests
{
    [Fact]
    public void Parse_Request_Get_WithHeadersAndBody()
    {
        var body = "name=Stone&age=30".GetBytes();
        var header = """
            GET /api/user?id=123 HTTP/1.1
            Host: example.com
            Connection: keep-alive
            Content-Length: 
            """ + body.Length + """

            Content-Type: application/x-www-form-urlencoded
            Custom: Value-1


            """;
        var raw = header.GetBytes();
        var pk = new ArrayPacket(raw) { Next = (ArrayPacket)body };

        var req = new HttpRequest();
        var ok = req.Parse(pk);
        Assert.True(ok);
        Assert.Equal("1.1", req.Version);
        Assert.Equal("GET", req.Method);
        Assert.Equal("/api/user?id=123", req.RequestUri + "");
        Assert.Equal("example.com", req.Host);
        Assert.True(req.KeepAlive);
        Assert.Equal(body.Length, req.ContentLength);
        Assert.Equal("application/x-www-form-urlencoded", req.ContentType);
        Assert.Equal(body.Length, req.BodyLength);
        Assert.True(req.IsCompleted);
        Assert.Equal("Value-1", req.Headers["Custom"]);
    }

    [Fact]
    public void Parse_Request_Post_NoContentLength()
    {
        var header = """
            POST /submit HTTP/1.0
            Host: test.com
            Connection: keep-alive
            Content-Type: text/plain


            """;
        var body = "Hello".GetBytes();
        var pk = new ArrayPacket(header.GetBytes()) { Next = (ArrayPacket)body };

        var req = new HttpRequest();
        var ok = req.Parse(pk);
        Assert.True(ok);
        Assert.Equal("1.0", req.Version);
        Assert.Equal("POST", req.Method);
        Assert.True(req.KeepAlive); // HTTP/1.0 keep-alive 由头部指定
        Assert.Equal(-1, req.ContentLength); // 未提供 Content-Length
        Assert.Equal(body.Length, req.BodyLength);
        Assert.True(req.IsCompleted); // 未指定长度视为完成
    }

    [Fact]
    public void FastParse_Request_OnlyFirstLine()
    {
        var data = "GET /ping HTTP/1.1\r\n".GetBytes();
        var pk = new ArrayPacket(data);
        var req = new HttpRequest();
        var ok = req.FastParse(pk);
        Assert.True(ok);
        Assert.Equal("GET", req.Method);
        Assert.Equal("/ping", req.RequestUri + "");
        Assert.Equal("1.1", req.Version);
        Assert.Null(req.Headers["Host"]);
    }

    [Fact(DisplayName = "Parse 链式跨节点头部：头部横跨多个节点仍可解析")]
    public void Parse_Chained_SplitHeader()
    {
        var raw = "POST /api/user HTTP/1.1\r\nHost: example.com\r\nContent-Length: 7\r\nContent-Type: text/plain\r\n\r\npayload".GetBytes();

        // 头部切到 3 个节点上（跨接收段组链）
        IPacket pk = new ArrayPacket(raw[..5]);
        pk.Append(new ArrayPacket(raw[5..30]));
        pk.Append(new ArrayPacket(raw[30..]));

        var req = new HttpRequest();
        Assert.True(req.Parse(pk));
        Assert.Equal("POST", req.Method);
        Assert.Equal("/api/user", req.RequestUri + "");
        Assert.Equal("example.com", req.Host);
        Assert.Equal(7, req.ContentLength);
        Assert.Equal("payload", req.Body?.ToStr());
    }

    [Fact(DisplayName = "FastParse 链式跨节点头部：首行横跨节点仍可解析")]
    public void FastParse_Chained_SplitFirstLine()
    {
        var raw = "GET /very/long/path HTTP/1.1\r\nHost: a\r\n\r\n".GetBytes();

        // 首行切到 3 个节点上
        IPacket pk = new ArrayPacket(raw[..7]);
        pk.Append(new ArrayPacket(raw[7..15]));
        pk.Append(new ArrayPacket(raw[15..]));

        var req = new HttpRequest();
        Assert.True(req.FastParse(pk));
        Assert.Equal("GET", req.Method);
        Assert.Equal("/very/long/path", req.RequestUri + "");
        Assert.Equal("1.1", req.Version);
    }

    [Fact]
    public void Build_Request_AutoMethodAndHost()
    {
        var req = new HttpRequest
        {
            RequestUri = new Uri("http://example.com:80/api/info?x=1"),
            KeepAlive = true,
            ContentType = "application/json",
            Body = (ArrayPacket)"{\"name\":\"Stone\"}".GetBytes()
        };
        var pk = req.Build();
        var text = pk.ToStr();
        Assert.StartsWith("POST /api/info?x=1 HTTP/1.1\r\n", text); // 有主体自动 POST
        Assert.Contains("Host: example.com\r\n", text);
        Assert.Contains("Content-Length: 16\r\n", text);
        Assert.Contains("Content-Type: application/json\r\n", text);
        Assert.Contains("Connection: keep-alive\r\n", text);
        Assert.EndsWith("\r\n{\"name\":\"Stone\"}", text);
    }

    [Fact(DisplayName = "Build 链式跨段体：全链逐段写入且长度一致")]
    public void Build_Request_ChainedBody()
    {
        var part1 = "{\"name\":\"Stone\"".GetBytes();
        var part2 = ",\"age\":18".GetBytes();
        var part3 = "}".GetBytes();

        IPacket chain = new ArrayPacket(part1);
        chain.Append(new ArrayPacket(part2));
        chain.Append(new ArrayPacket(part3));

        var total = part1.Length + part2.Length + part3.Length;
        var req = new HttpRequest
        {
            RequestUri = new Uri("http://example.com/api/info"),
            ContentType = "application/json",
            Body = chain
        };

        var pk = req.Build();
        var text = pk.ToStr();
        Assert.Contains($"Content-Length: {total}\r\n", text);
        Assert.EndsWith("{\"name\":\"Stone\",\"age\":18}", text);

        // 回读验证：主体与声明长度一致
        var req2 = new HttpRequest();
        Assert.True(req2.Parse(pk));
        Assert.Equal(total, req2.ContentLength);
        Assert.Equal("{\"name\":\"Stone\",\"age\":18}", req2.Body!.ToStr());
    }

    [Fact]
    public void Parse_InvalidHeader_ReturnsFalse()
    {
        var raw = """
            INVALID_HEADER
            Key:Value


            """.GetBytes();
        var pk = new ArrayPacket(raw);
        var req = new HttpRequest();
        var ok = req.Parse(pk);
        Assert.False(ok);
    }

    [Fact(DisplayName = "ParseFormData 链式跨段：多字节字符被段边界截断仍正确解码")]
    public void ParseFormData_Chained_MultiByteSplit()
    {
        var head = "------WebKitFormBoundary3ZXeqQWNjAzojVR7\r\n" +
            "Content-Disposition: form-data; name=\"name\"\r\n\r\n" +
            "大石头\r\n" +
            "------WebKitFormBoundary3ZXeqQWNjAzojVR7\r\n" +
            "Content-Disposition: form-data; name=\"avatar\"; filename=\"logo.bin\"\r\n" +
            "Content-Type: application/octet-stream\r\n\r\n";
        var fileData = new Byte[256];
        for (var i = 0; i < fileData.Length; i++) fileData[i] = (Byte)i;
        var tail = "\r\n------WebKitFormBoundary3ZXeqQWNjAzojVR7--\r\n";

        var raw = head.GetBytes();
        // 把“大石头”的 UTF-8 字节切在字符中间（首字节在上一段，其余字节在下一段）
        var mark = "大石头".GetBytes();
        var pos = raw.AsSpan().IndexOf(mark);
        Assert.True(pos > 0);
        var split = pos + 1;

        IPacket pk = new ArrayPacket(raw[..split]);
        pk.Append(new ArrayPacket(raw[split..]));
        pk.Append(new ArrayPacket(fileData[..100]));
        pk.Append(new ArrayPacket(fileData[100..]));
        pk.Append(new ArrayPacket(tail.GetBytes()));

        var req = new HttpRequest
        {
            ContentType = "multipart/form-data;boundary=----WebKitFormBoundary3ZXeqQWNjAzojVR7",
            Body = pk
        };

        var dic = req.ParseFormData();
        Assert.Equal("大石头", dic["name"]);

        var av = dic["avatar"] as FormFile;
        Assert.NotNull(av);
        Assert.Equal("logo.bin", av.FileName);
        Assert.Equal((Int64)fileData.Length, av.Length);
        Assert.Equal(fileData, av.OpenReadStream()!.ReadBytes(-1));
    }

    [Fact(DisplayName = "ParseFormData 池化主体：文本字段不残留切片，文件数据共享引用")]
    public void ParseFormData_OwnerBody_NoLeak()
    {
        var raw = ("------XBoundary\r\n" +
            "Content-Disposition: form-data; name=\"name\"\r\n\r\n" +
            "大石头\r\n" +
            "------XBoundary\r\n" +
            "Content-Disposition: form-data; name=\"avatar\"; filename=\"logo.bin\"\r\n" +
            "Content-Type: application/octet-stream\r\n\r\n" +
            "FILE-DATA-0123456789\r\n" +
            "------XBoundary--\r\n").GetBytes();

        var body = new OwnerPacket(raw.Length);
        raw.CopyTo(body.GetSpan());

        var req = new HttpRequest
        {
            ContentType = "multipart/form-data;boundary=----XBoundary",
            Body = body
        };

        var dic = req.ParseFormData();
        Assert.Equal("大石头", dic["name"]);

        var av = dic["avatar"] as FormFile;
        Assert.NotNull(av);
        Assert.Equal("FILE-DATA-0123456789", av.OpenReadStream()!.ReadBytes(-1).AsSpan().ToStr());

        // 主体句柄(1) + 文件数据共享切片(1)；文本字段不应残留句柄
        Assert.Equal(2, body.RefCount);

        // 释放主体句柄后，文件数据仍可读取（引用计数共享）
        req.Body = null;
        body.Dispose();
        Assert.Equal("FILE-DATA-0123456789", av.OpenReadStream()!.ReadBytes(-1).AsSpan().ToStr());

        av.Data.TryDispose();
    }

    [Fact(DisplayName = "ParseFormData 带引号 boundary：仍可正确解析")]
    public void ParseFormData_QuotedBoundary()
    {
        var boundary = "----MyBoundary42";
        var raw = ($"--{boundary}\r\n" +
            "Content-Disposition: form-data; name=\"name\"\r\n\r\n" +
            "大石头\r\n" +
            $"--{boundary}--\r\n").GetBytes();

        var req = new HttpRequest
        {
            ContentType = $"multipart/form-data; boundary=\"{boundary}\"",
            Body = new ArrayPacket(raw)
        };

        var dic = req.ParseFormData();
        Assert.Equal("大石头", dic["name"]);
    }

    [Fact(DisplayName = "ParseFormData 空文本值与零字节文件：正常解析")]
    public void ParseFormData_EmptyValue_And_EmptyFile()
    {
        var boundary = "----EmptyBd";
        var head = $"--{boundary}\r\n" +
            "Content-Disposition: form-data; name=\"note\"\r\n\r\n" +   // 空文本值
            "\r\n" +
            $"--{boundary}\r\n" +
            "Content-Disposition: form-data; name=\"avatar\"; filename=\"empty.bin\"\r\n" +
            "Content-Type: application/octet-stream\r\n\r\n";           // 零字节文件
        var tail = $"\r\n--{boundary}--\r\n";

        // 链式：文本区在节点1，尾标记在节点2
        IPacket pk = new ArrayPacket(head.GetBytes());
        pk.Append(new ArrayPacket(tail.GetBytes()));

        var req = new HttpRequest
        {
            ContentType = $"multipart/form-data; boundary={boundary}",
            Body = pk
        };

        var dic = req.ParseFormData();
        Assert.Equal("", dic["note"]);

        var av = dic["avatar"] as FormFile;
        Assert.NotNull(av);
        Assert.Equal(0L, av.Length);
        Assert.True(av.IsEmpty);
        Assert.Empty(av.OpenReadStream()!.ReadBytes(-1));
    }
}
