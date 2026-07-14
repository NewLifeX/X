using System.Net;
using System.Reflection;
using NewLife.Data;

namespace NewLife.Http;

/// <summary>内嵌资源文件处理器</summary>
/// <remarks>
/// 将程序集中的嵌入资源映射为 HTTP 静态文件服务。
/// 
/// 例如注册 /panel/ 对应资源前缀 MyApp.Resources.Panel，
/// 则请求 GET /panel/css/style.css 将加载嵌入资源 MyApp.Resources.Panel.css.style.css
/// </remarks>
public class EmbeddedFileHandler : IHttpHandler
{
    #region 属性
    /// <summary>映射路径。如 /panel/</summary>
    public String Path { get; set; } = null!;

    /// <summary>资源名前缀。如 MyApp.Resources.Panel</summary>
    public String ContentPath { get; set; } = null!;

    /// <summary>资源所在的程序集。为 null 时使用入口程序集</summary>
    public Assembly? Assembly { get; set; }

    private static readonly Dictionary<String, String> _mimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".json"] = "application/json",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
    };
    #endregion

    /// <summary>处理请求</summary>
    /// <param name="ctx">Http上下文</param>
    public virtual void ProcessRequest(IHttpContext ctx)
    {
        var file = ctx.Path;
        if (file.StartsWithIgnoreCase(Path))
            file = file[Path.Length..];

        // 默认文档
        if (file.IsNullOrEmpty() || file == "/")
            file = "index.html";

        // 安全：防止路径穿越
        if (file.Contains(".."))
        {
            ctx.Response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        // 获取目标程序集
        var asm = Assembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        if (asm == null)
        {
            ctx.Response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        // 将路径转换为嵌入资源名称（/ → .）
        var resourceName = $"{ContentPath}.{file.Replace('/', '.').Replace('\\', '.')}";

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            ctx.Response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var ext = System.IO.Path.GetExtension(file) ?? "";
        var contentType = _mimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";

        ctx.Response.SetResult(stream, contentType);
    }
}
