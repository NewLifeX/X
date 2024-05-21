namespace NewLife.Serialization;

/// <summary>Json序列化选项</summary>
public class JsonOptions
{
    /// <summary>使用驼峰命名。默认false</summary>
    public Boolean CamelCase { get; set; }

    /// <summary>忽略空值。默认false</summary>
    public Boolean IgnoreNullValues { get; set; }

    /// <summary>忽略循环引用。遇到循环引用时写{}，默认false</summary>
    public Boolean IgnoreCycles { get; set; }

    /// <summary>缩进。默认false</summary>
    public Boolean WriteIndented { get; set; }

    /// <summary>使用完整的时间格式。如：2022-11-29T14:13:17.8763881+08:00，默认false</summary>
    public Boolean FullTime { get; set; }
}
