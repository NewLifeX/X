namespace NewLife.Xml;

/// <summary>Xml配置文件特性</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class XmlConfigFileAttribute : Attribute
{
    /// <summary>配置文件名</summary>
    public String FileName { get; set; }

    /// <summary>重新加载时间。单位：毫秒</summary>
    public Int32 ReloadTime { get; set; }

    /// <summary>指定配置文件名</summary>
    /// <param name="fileName"></param>
    public XmlConfigFileAttribute(String fileName) => FileName = fileName;

    /// <summary>指定配置文件名和重新加载时间（毫秒）</summary>
    /// <param name="fileName"></param>
    /// <param name="reloadTime"></param>
    public XmlConfigFileAttribute(String fileName, Int32 reloadTime)
    {
        FileName = fileName;
        ReloadTime = reloadTime;
    }
}