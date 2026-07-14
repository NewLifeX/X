namespace NewLife.Http;

/// <summary>共享MIME类型映射，供StaticFilesHandler和EmbeddedFileHandler共用</summary>
internal static class MimeHelper
{
    private static readonly Dictionary<String, String> _mimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // 文本类型
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".txt"] = "text/plain; charset=utf-8",
        [".log"] = "text/plain; charset=utf-8",
        [".xml"] = "text/xml; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".csv"] = "text/csv; charset=utf-8",

        // 脚本和数据
        [".js"] = "text/javascript; charset=utf-8",
        [".json"] = "application/json",
        [".map"] = "application/json",

        // 图片类型
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".ico"] = "image/x-icon",
        [".svg"] = "image/svg+xml",
        [".webp"] = "image/webp",

        // 字体类型
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".eot"] = "application/vnd.ms-fontobject",

        // 二进制类型
        [".zip"] = "application/zip",
        [".pdf"] = "application/pdf",
    };

    /// <summary>根据文件扩展名获取MIME类型</summary>
    /// <param name="extension">文件扩展名（含点号，如 ".html"）</param>
    /// <returns>MIME类型；未知类型返回null</returns>
    public static String? GetContentType(String extension)
    {
        if (extension.IsNullOrEmpty()) return null;

        return _mimeTypes.TryGetValue(extension, out var mime) ? mime : null;
    }
}
