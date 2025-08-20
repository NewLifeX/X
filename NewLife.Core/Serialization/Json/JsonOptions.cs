namespace NewLife.Serialization;

/// <summary>Json序列化选项</summary>
public class JsonOptions
{
    #region 属性
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

    /// <summary>枚举使用字符串。默认false使用数字</summary>
    public Boolean EnumString { get; set; }

    /// <summary>长整型作为字符串序列化。避免长整型传输给前端时精度丢失，只有值真的超过前端接受范围时才会进行转换，默认false</summary>
    public Boolean Int64AsString { get; set; }
    #endregion

    #region 构造
    /// <summary>默认构造函数</summary>
    public JsonOptions() { }

    /// <summary>复制构造函数</summary>
    public JsonOptions(JsonOptions jsonOptions)
    {
        CamelCase = jsonOptions.CamelCase;
        IgnoreNullValues = jsonOptions.IgnoreNullValues;
        IgnoreCycles = jsonOptions.IgnoreCycles;
        WriteIndented = jsonOptions.WriteIndented;
        FullTime = jsonOptions.FullTime;
        EnumString = jsonOptions.EnumString;
        Int64AsString = jsonOptions.Int64AsString;
    }
    #endregion
}
