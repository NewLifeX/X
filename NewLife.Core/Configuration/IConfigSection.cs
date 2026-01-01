namespace NewLife.Configuration;

/// <summary>配置对象</summary>
/// <remarks>表示配置树中的一个节点，包含键、值、注释及子节点</remarks>
public interface IConfigSection
{
    /// <summary>配置名</summary>
    String Key { get; set; }

    /// <summary>配置值</summary>
    String? Value { get; set; }

    /// <summary>注释</summary>
    String? Comment { get; set; }

    /// <summary>子级</summary>
    IList<IConfigSection>? Childs { get; set; }

    /// <summary>获取或设置配置值</summary>
    /// <param name="key">配置名，支持冒号分隔的多级名称</param>
    /// <returns>对应的配置值；未找到时返回 null</returns>
    String? this[String key] { get; set; }
}

/// <summary>配置项</summary>
/// <remarks>配置对象的默认实现</remarks>
public class ConfigSection : IConfigSection
{
    #region 属性
    /// <summary>配置名</summary>
    public String Key { get; set; } = null!;

    /// <summary>配置值</summary>
    public String? Value { get; set; }

    /// <summary>注释</summary>
    public String? Comment { get; set; }

    /// <summary>子级</summary>
    public IList<IConfigSection>? Childs { get; set; }
    #endregion

    #region 方法
    /// <summary>获取或设置配置值</summary>
    /// <param name="key">配置名，支持冒号分隔的多级名称</param>
    /// <returns>对应的配置值；未找到时返回 null</returns>
    public virtual String? this[String key]
    {
        get => this.Find(key, false)?.Value;
        set
        {
            var section = this.Find(key, true);
            section?.Value = value;
        }
    }

    /// <summary>已重载。返回配置项的友好字符串表示</summary>
    /// <returns>如有子级返回 "Key[Count]"，否则返回 "Key=Value"</returns>
    public override String ToString() => Childs != null && Childs.Count > 0 ? $"{Key}[{Childs.Count}]" : $"{Key}={Value}";
    #endregion
}