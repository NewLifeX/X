using NewLife.Data;

namespace NewLife.Http;

/// <summary>表单部分</summary>
public class FormFile
{
    #region 属性
    /// <summary>名称</summary>
    public String? Name { get; set; }

    /// <summary>内容部署</summary>
    public String? ContentDisposition { get; set; }

    /// <summary>内容类型</summary>
    public String? ContentType { get; set; }

    /// <summary>文件名</summary>
    public String? FileName { get; set; }

    /// <summary>数据</summary>
    public Packet? Data { get; set; }

    /// <summary>长度</summary>
    public Int64 Length => Data?.Total ?? 0;
    #endregion

    /// <summary>打开数据读取流</summary>
    /// <returns></returns>
    public Stream? OpenReadStream() => Data?.GetStream();

    /// <summary>保存到文件</summary>
    /// <param name="fileName"></param>
    public void SaveToFile(String? fileName = null)
    {
        if (fileName.IsNullOrEmpty()) fileName = FileName;
        if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));
        if (Data == null) throw new ArgumentNullException(nameof(Data));

        fileName.EnsureDirectory(true);

        using var fs = File.OpenWrite(fileName.GetFullPath());
        Data.CopyTo(fs);
        fs.SetLength(fs.Position);
    }
}