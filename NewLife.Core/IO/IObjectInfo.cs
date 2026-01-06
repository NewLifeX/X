using System.Diagnostics;
using NewLife.Data;

namespace NewLife.IO;

/// <summary>对象信息接口。代表文件存储对象，或者磁盘文件，也可以是目录</summary>
public interface IObjectInfo
{
    /// <summary>名称。文件名或目录名，可以包含路径</summary>
    String? Name { get; set; }

    /// <summary>大小。文件字节数，目录时为0</summary>
    Int64 Length { get; set; }

    /// <summary>时间。文件或目录的最后修改时间</summary>
    DateTime Time { get; set; }

    /// <summary>是否目录。为true时表示目录，为false时表示文件</summary>
    Boolean IsDirectory { get; set; }

    /// <summary>内容类型。如 image/png、application/json 等 MIME 类型</summary>
    String? ContentType { get; set; }

    /// <summary>哈希值。文件内容的哈希摘要，常用 MD5 或 SHA256</summary>
    String? Hash { get; set; }

    /// <summary>访问地址。文件的直接访问Url，部分存储支持</summary>
    String? Url { get; set; }

    /// <summary>原始地址。文件的原始访问Url，未经签名的公开地址</summary>
    String? RawUrl { get; set; }

    /// <summary>数据。文件内容数据包</summary>
    IPacket? Data { get; set; }
}

/// <summary>对象信息。代表文件存储对象，或者磁盘文件，也可以是目录</summary>
[DebuggerDisplay("{Name} [{Length}]")]
public class ObjectInfo : IObjectInfo
{
    #region 属性
    /// <summary>名称。文件名或目录名，可以包含路径</summary>
    public String? Name { get; set; }

    /// <summary>大小。文件字节数，目录时为0</summary>
    public Int64 Length { get; set; }

    /// <summary>时间。文件或目录的最后修改时间</summary>
    public DateTime Time { get; set; }

    /// <summary>是否目录。为true时表示目录，为false时表示文件</summary>
    public Boolean IsDirectory { get; set; }

    /// <summary>内容类型。如 image/png、application/json 等 MIME 类型</summary>
    public String? ContentType { get; set; }

    /// <summary>哈希值。文件内容的哈希摘要，常用 MD5 或 SHA256</summary>
    public String? Hash { get; set; }

    /// <summary>访问地址。文件的直接访问Url，部分存储支持</summary>
    public String? Url { get; set; }

    /// <summary>原始地址。文件的原始访问Url，未经签名的公开地址</summary>
    public String? RawUrl { get; set; }

    /// <summary>数据。文件内容数据包</summary>
    public IPacket? Data { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化对象信息</summary>
    public ObjectInfo() { }

    /// <summary>实例化对象信息</summary>
    /// <param name="name">文件名或目录名</param>
    public ObjectInfo(String name) => Name = name;

    /// <summary>实例化对象信息</summary>
    /// <param name="name">文件名或目录名</param>
    /// <param name="data">文件内容数据包</param>
    public ObjectInfo(String name, IPacket data)
    {
        Name = name;
        Data = data;
        Length = data.Length;
    }
    #endregion

    #region 方法
    /// <summary>从文件信息创建对象信息</summary>
    /// <param name="file">文件信息</param>
    /// <returns>对象信息</returns>
    public static ObjectInfo FromFile(FileInfo file)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));

        return new ObjectInfo
        {
            Name = file.Name,
            Length = file.Exists ? file.Length : 0,
            Time = file.Exists ? file.LastWriteTime : DateTime.MinValue,
            IsDirectory = false,
            ContentType = GetContentType(file.Extension)
        };
    }

    /// <summary>从目录信息创建对象信息</summary>
    /// <param name="directory">目录信息</param>
    /// <returns>对象信息</returns>
    public static ObjectInfo FromDirectory(DirectoryInfo directory)
    {
        if (directory == null) throw new ArgumentNullException(nameof(directory));

        return new ObjectInfo
        {
            Name = directory.Name,
            Length = 0,
            Time = directory.Exists ? directory.LastWriteTime : DateTime.MinValue,
            IsDirectory = true
        };
    }

    /// <summary>根据文件扩展名获取内容类型</summary>
    /// <param name="extension">文件扩展名，如 .png</param>
    /// <returns>MIME 类型</returns>
    public static String? GetContentType(String? extension)
    {
        if (extension.IsNullOrEmpty()) return null;

        extension = extension.ToLowerInvariant().TrimStart('.');
        return extension switch
        {
            // 图片
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "webp" => "image/webp",
            "ico" => "image/x-icon",
            "svg" => "image/svg+xml",

            // 文档
            "html" or "htm" => "text/html",
            "css" => "text/css",
            "js" => "application/javascript",
            "json" => "application/json",
            "xml" => "application/xml",
            "txt" => "text/plain",
            "csv" => "text/csv",
            "md" => "text/markdown",

            // 办公文档
            "pdf" => "application/pdf",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ppt" => "application/vnd.ms-powerpoint",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",

            // 压缩包
            "zip" => "application/zip",
            "rar" => "application/x-rar-compressed",
            "7z" => "application/x-7z-compressed",
            "tar" => "application/x-tar",
            "gz" => "application/gzip",

            // 音视频
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "mp4" => "video/mp4",
            "avi" => "video/x-msvideo",
            "mov" => "video/quicktime",
            "webm" => "video/webm",

            // 字体
            "woff" => "font/woff",
            "woff2" => "font/woff2",
            "ttf" => "font/ttf",
            "otf" => "font/otf",

            // 默认
            _ => "application/octet-stream"
        };
    }

    /// <summary>已重载。返回对象名称和大小</summary>
    /// <returns>字符串表示</returns>
    public override String ToString() => IsDirectory ? $"{Name}/" : $"{Name} [{Length:n0}]";
    #endregion
}