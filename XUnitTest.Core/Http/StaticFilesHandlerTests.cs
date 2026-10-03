using System.Text;
using NewLife;
using NewLife.Http;
using NewLife.Net;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Http;

/// <summary>静态文件处理器测试</summary>
public class StaticFilesHandlerTests
{
    private static String CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nl_static_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        return root;
    }

    private static DefaultHttpContext CreateContext(String path, StaticFilesHandler handler) => new((INetSession?)null, new HttpRequest(), path, handler);

    [Fact(DisplayName = "静态文件_同前缀兄弟目录_拒绝访问")]
    public void ProcessRequest_SiblingDirectorySamePrefix_NotFound()
    {
        var root = CreateTempRoot();
        var js = Path.Combine(root, "js");
        var js2 = Path.Combine(root, "js2");
        Directory.CreateDirectory(js);
        Directory.CreateDirectory(js2);
        File.WriteAllText(Path.Combine(js2, "b.js"), "alert(1)");

        try
        {
            var handler = new StaticFilesHandler { Path = "/js/", ContentPath = js };
            var ctx = CreateContext("/js/../js2/b.js", handler);

            // 内容目录 ...\js 与 ...\js2 是同前缀兄弟目录：只按字符串前缀比对会放行而穿透，
            // 基路径补上目录分隔符后必须拒绝
            Assert.Throws<ApiException>(() => handler.ProcessRequest(ctx));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact(DisplayName = "静态文件_百分号编码的父目录_不穿越")]
    public void ProcessRequest_EncodedParentDirectory_NotTraversed()
    {
        var root = CreateTempRoot();
        var js = Path.Combine(root, "js");
        Directory.CreateDirectory(js);
        // 基目录之外放一个"机密"文件，若编码形态被解码成 .. 就会读到它
        File.WriteAllText(Path.Combine(root, "secret.txt"), "top secret");

        try
        {
            var handler = new StaticFilesHandler { Path = "/js/", ContentPath = js };
            // 路径全程未做百分号解码：%2e%2e%2f 不会还原成 ".."，只会落到"文件不存在"
            var ctx = CreateContext("/js/%2e%2e%2fsecret.txt", handler);

            Assert.Throws<ApiException>(() => handler.ProcessRequest(ctx));
            Assert.Null(ctx.Response.BodyStream);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact(DisplayName = "静态文件_目录内文件_正常提供")]
    public void ProcessRequest_InsideDirectory_Serves()
    {
        var root = CreateTempRoot();
        var js = Path.Combine(root, "js");
        Directory.CreateDirectory(js);
        File.WriteAllText(Path.Combine(js, "a.js"), "var a = 1;");

        try
        {
            var handler = new StaticFilesHandler { Path = "/js/", ContentPath = js };
            var ctx = CreateContext("/js/a.js", handler);

            handler.ProcessRequest(ctx);

            // 回归护栏：补分隔符后不得把内容目录内的正常文件也挡在外面
            Assert.NotNull(ctx.Response.BodyStream);
            using (ctx.Response.BodyStream)
            {
                Assert.Equal("var a = 1;", Encoding.UTF8.GetString(ctx.Response.BodyStream.ReadBytes(-1)));
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
