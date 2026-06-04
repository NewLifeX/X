using System.Net;
using System.Net.Http.Headers;
using System.Text;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

/// <summary>DefaultTracerResolver 单元测试</summary>
public class TracerResolverTests
{
    /// <summary>创建带采样空间的跟踪器</summary>
    private static DefaultTracer CreateTracer() => new()
    {
        MaxSamples = 100,
        MaxErrors = 100
    };

    [Fact(DisplayName = "ByteArrayContent JSON 请求体读取前缀")]
    public void ByteArrayContent_Json_Prefix()
    {
        var tracer = CreateTracer();
        var json = """{"name":"stone","age":18,"extra":"data"}""";
        var bytes = Encoding.UTF8.GetBytes(json);
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api/echo") { Content = content };
        using var span = tracer.NewSpan(request);

        Assert.NotNull(span);
        Assert.Contains("name\":\"stone", span.Tag);
        Assert.Contains("age\":18", span.Tag);
    }

    [Fact(DisplayName = "StringContent XML 请求体读取前缀")]
    public void StringContent_Xml_Prefix()
    {
        var tracer = CreateTracer();
        var xml = "<root><name>stone</name></root>";
        using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api/xml") { Content = content };
        using var span = tracer.NewSpan(request);

        Assert.NotNull(span);
        Assert.Contains("<root>", span.Tag);
        Assert.Contains("<name>stone</name>", span.Tag);
    }

    [Fact(DisplayName = "FormUrlEncodedContent 表单请求体读取前缀")]
    public void FormUrlEncoded_Content_Prefix()
    {
        var tracer = CreateTracer();
        using var content = new FormUrlEncodedContent(new Dictionary<String, String>
        {
            ["name"] = "stone",
            ["age"] = "18"
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api/form") { Content = content };
        using var span = tracer.NewSpan(request);

        Assert.NotNull(span);
        Assert.Contains("name=stone", span.Tag);
        Assert.Contains("age=18", span.Tag);
    }

    [Fact(DisplayName = "MultipartFormDataContent 查找文本部分前缀")]
    public void MultipartFormData_TextPart_Prefix()
    {
        var tracer = CreateTracer();
        using var multipart = new MultipartFormDataContent();
        // 文本部分优先，应被 FindFirstTextContent 找到
        multipart.Add(new StringContent("name=stone&age=18", Encoding.UTF8, "application/x-www-form-urlencoded"), "form");
        // 文件部分（二进制，无 ContentType），应被跳过
        multipart.Add(new ByteArrayContent([0x00, 0x01, 0x02, 0x03, 0x04]), "file", "test.bin");

        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api/upload") { Content = multipart };
        using var span = tracer.NewSpan(request);

        Assert.NotNull(span);
        // 应包含文本部分内容
        Assert.Contains("name=stone", span.Tag);
        Assert.Contains("age=18", span.Tag);
    }

    [Fact(DisplayName = "大请求体读取前缀（已移除大小限制）")]
    public void LargeBody_Prefix()
    {
        var tracer = CreateTracer();
        // 10KB 内容，验证移除大小限制后仍能读取前缀
        var largePart = new String('x', 1024 * 10);
        var body = $"{{\"data\":\"{largePart}\"}}";
        Assert.True(body.Length > 1024 * 8, "请求体应超过 8KB（验证大小限制已移除）");

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api/echo") { Content = content };
        using var span = tracer.NewSpan(request);

        Assert.NotNull(span);
        // 现在应该包含请求体前缀
        Assert.Contains("data", span.Tag);
        // 前缀不应超过 maxLength（默认 1024）
        Assert.True(span.Tag.Length < 2048, "Tag 长度应受 MaxTagLength 约束");
    }

    [Fact(DisplayName = "非文本 ContentType 不读取内容")]
    public void NonTextContentType_Skip()
    {
        var tracer = CreateTracer();
        var bytes = "not real png"u8.ToArray();
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api/upload") { Content = content };
        using var span = tracer.NewSpan(request);

        Assert.NotNull(span);
        // 应以 method + uri 开头
        Assert.StartsWith("POST http://test/api/upload", span.Tag);
        // 不应包含读取的内容体（因为 image/png 不在 TagTypes 中）
        Assert.DoesNotContain("not real png", span.Tag);
    }

    [Fact(DisplayName = "RequestContentAsTag 关闭时不读取内容")]
    public void RequestContentAsTag_Disabled()
    {
        var tracer = CreateTracer();
        // 关闭内容标签
        if (tracer.Resolver is DefaultTracerResolver resolver)
            resolver.RequestContentAsTag = false;

        var json = """{"name":"stone"}""";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api/echo") { Content = content };
        using var span = tracer.NewSpan(request);

        Assert.NotNull(span);
        // 不应包含请求体内容
        Assert.DoesNotContain("name\":\"stone", span.Tag);
    }
}
