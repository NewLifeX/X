using NewLife.Remoting;

namespace NewLife.Http;

/// <summary>静态文件处理器</summary>
/// <remarks>将指定目录映射为静态文件服务，支持常见的文件类型</remarks>
public class StaticFilesHandler : IHttpHandler
{
    #region 属性
    /// <summary>映射路径。如 /js/</summary>
    public String Path { get; set; } = null!;

    /// <summary>内容目录。如 /wwwroot/js</summary>
    public String ContentPath { get; set; } = null!;
    #endregion

    /// <summary>处理请求</summary>
    /// <param name="context">Http上下文</param>
    public virtual void ProcessRequest(IHttpContext context)
    {
        if (!context.Path.StartsWithIgnoreCase(Path)) throw new ApiException(ApiCode.NotFound, $"File {context.Path} not found");

        var file = context.Path[Path.Length..];
        file = ContentPath.CombinePath(file);

        // 路径安全检查，防止目录穿越
        if (!file.GetFullPath().StartsWithIgnoreCase(ContentPath.GetFullPath()))
            throw new ApiException(ApiCode.NotFound, $"File {context.Path} not found");

        var fi = file.AsFile();
        if (!fi.Exists) throw new ApiException(ApiCode.NotFound, $"File {context.Path} not found");

        var contentType = GetContentType(fi.Extension);

        // 确保使用完以后关闭文件流
        using var fs = fi.OpenRead();
        context.Response.SetResult(fs, contentType);
    }

    /// <summary>根据文件扩展名获取MIME类型</summary>
    /// <param name="extension">文件扩展名（含点号）</param>
    /// <returns>MIME类型；未知类型返回null</returns>
    protected virtual String? GetContentType(String extension) => MimeHelper.GetContentType(extension);
}