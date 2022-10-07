namespace NewLife.Configuration;

/// <summary>配置对象</summary>
public interface IConfigSection
{
    /// <summary>配置名</summary>
    String Key { get; set; }

    /// <summary>配置值</summary>
    String Value { get; set; }

    /// <summary>注释</summary>
    String Comment { get; set; }

    /// <summary>子级</summary>
    IList<IConfigSection> Childs { get; set; }

    /// <summary>获取 或 设置 配置值</summary>
    /// <param name="key">配置名，支持冒号分隔的多级名称</param>
    /// <returns></returns>
    String this[String key] { get; set; }
}

/// <summary>配置项</summary>
public class ConfigSection : IConfigSection
{
    #region 属性
    /// <summary>配置名</summary>
    public String Key { get; set; }

    /// <summary>配置值</summary>
    public String Value { get; set; }

    /// <summary>注释</summary>
    public String Comment { get; set; }

    /// <summary>子级</summary>
    public IList<IConfigSection> Childs { get; set; }
    #endregion

    #region 方法
    /// <summary>获取 或 设置 配置值</summary>
    /// <param name="key">键</param>
    /// <returns></returns>
    public virtual String this[String key]
    {
        get => this.Find(key, false)?.Value;
        set => this.Find(key, true).Value = value;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => Childs != null && Childs.Count > 0 ? $"{Key}[{Childs.Count}]" : $"{Key}={Value}";
    #endregion
}