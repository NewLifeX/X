using System.Diagnostics;
using NewLife.Data;

namespace NewLife.IO;

/// <summary>对象信息接口。代表文件存储对象，或者磁盘文件，也可以是目录</summary>
public interface IObjectInfo
{
    /// <summary>名称</summary>
    String? Name { get; set; }

    /// <summary>大小</summary>
    Int64 Length { get; set; }

    /// <summary>时间</summary>
    DateTime Time { get; set; }

    /// <summary>是否目录</summary>
    Boolean IsDirectory { get; set; }

    /// <summary>数据</summary>
    Packet? Data { get; set; }
}

/// <summary>对象信息。代表文件存储对象，或者磁盘文件，也可以是目录</summary>
[DebuggerDisplay("{Name} [{Length}]")]
public class ObjectInfo : IObjectInfo
{
    /// <summary>名称</summary>
    public String? Name { get; set; }

    /// <summary>大小</summary>
    public Int64 Length { get; set; }

    /// <summary>时间</summary>
    public DateTime Time { get; set; }

    /// <summary>是否目录</summary>
    public Boolean IsDirectory { get; set; }

    /// <summary>数据</summary>
    public Packet? Data { get; set; }
}