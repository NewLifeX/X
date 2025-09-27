namespace NewLife.Http;

/// <summary>表单部分</summary>
public class FormFile
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; } = null!;

    /// <summary>内容描述（Content-Disposition）</summary>
    public String? ContentDisposition { get; set; }

    /// <summary>内容类型</summary>
    public String? ContentType { get; set; }

    /// <summary>文件名（可能包含路径或用户提交的原始名称，需注意安全）</summary>
    public String? FileName { get; set; }

    /// <summary>数据</summary>
    public Byte[]? Data { get; set; }

    /// <summary>长度</summary>
    public Int64 Length => Data?.Length ?? 0;

    /// <summary>是否为空（无数据或长度为0）</summary>
    public Boolean IsEmpty => Length == 0;
    #endregion

    #region 读取
    /// <summary>打开数据读取流</summary>
    /// <returns>内存流；如果无数据返回 null</returns>
    public Stream? OpenReadStream() => Data == null ? null : new MemoryStream(Data, false);

    /// <summary>复制数据到目标流</summary>
    /// <param name="destination">目标流</param>
    public void WriteTo(Stream destination)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (Data == null || Data.Length == 0) return;

        // 直接写入底层缓冲，避免再包装 MemoryStream 带来额外开销
        destination.Write(Data, 0, Data.Length);
    }
    #endregion

    #region 文件保存
    /// <summary>获取安全文件名（去除路径，仅文件名部分，可选字符清理）</summary>
    /// <param name="strict">是否替换非法字符为下划线</param>
    /// <returns>安全文件名；如果原始文件名为空则返回 null</returns>
    public String? GetSafeFileName(Boolean strict = true)
    {
        if (FileName.IsNullOrEmpty()) return null;

        // 去除用户可能提交的路径（防目录穿越）
        var name = Path.GetFileName(FileName);
        if (name.IsNullOrEmpty()) return null;

        if (strict)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                if (name!.IndexOf(c) >= 0) name = name.Replace(c, '_');
            }
        }
        return name;
    }

    /// <summary>保存到文件（保持向后兼容的旧签名）。文件名为空时使用 <see cref="FileName"/>；内部自动截断旧文件。</summary>
    /// <param name="fileName">目标文件名（可为相对/绝对路径）。</param>
    public void SaveToFile(String? fileName = null)
    {
        // 兼容旧逻辑：直接调用内部实现，允许覆盖
        SaveToFile(fileName, overwrite: true, sanitize: false);
    }

    /// <summary>保存到文件（可控是否覆盖与是否清理文件名）。</summary>
    /// <param name="fileName">目标文件名；为空则使用 <see cref="FileName"/></param>
    /// <param name="overwrite">是否允许覆盖已存在文件</param>
    /// <param name="sanitize">是否对文件名做安全处理（仅在使用自身 FileName 且未显式提供 fileName 时生效）</param>
    public void SaveToFile(String? fileName, Boolean overwrite, Boolean sanitize)
    {
        if (fileName.IsNullOrEmpty())
        {
            fileName = sanitize ? GetSafeFileName() : Path.GetFileName(FileName);
        }
        if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));
        if (Data == null) throw new ArgumentNullException(nameof(Data));

        InternalSave(fileName!, overwrite);
    }

    /// <summary>保存到指定目录，使用安全文件名。</summary>
    /// <param name="directory">目标目录</param>
    /// <param name="overwrite">是否覆盖已有文件</param>
    public String SaveToDirectory(String directory, Boolean overwrite = false)
    {
        if (directory.IsNullOrEmpty()) throw new ArgumentNullException(nameof(directory));
        var name = GetSafeFileName() ?? throw new ArgumentNullException(nameof(FileName));

        directory.EnsureDirectory(false);
        var full = Path.Combine(directory, name);
        SaveToFile(full, overwrite, sanitize: false);
        return full;
    }

    private void InternalSave(String fileName, Boolean overwrite)
    {
        fileName.EnsureDirectory(true);
        var full = fileName.GetFullPath();
        if (!overwrite && File.Exists(full)) throw new IOException("目标文件已存在且不允许覆盖：" + full);

        // 使用 FileMode.Create 统一（自动截断旧文件），避免旧实现写短文件后尾部残留的风险
        using var fs = File.Open(full, FileMode.Create, FileAccess.Write, FileShare.None);
        if (Data != null && Data.Length > 0)
        {
            fs.Write(Data, 0, Data.Length);
        }
        fs.Flush();
    }
    #endregion
}