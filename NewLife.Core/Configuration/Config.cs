using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife.Log;

namespace NewLife.Configuration;

/// <summary>配置文件基类</summary>
/// <remarks>
/// 标准用法：TConfig.Current
/// 
/// 配置实体类通过<see cref="ConfigAttribute"/>特性指定配置文件路径。
/// Current将加载配置文件，如果文件不存在或者加载失败，将实例化一个对象返回。
/// </remarks>
/// <typeparam name="TConfig"></typeparam>
public class Config<TConfig> where TConfig : Config<TConfig>, new()
{
    #region 静态
    /// <summary>当前使用的提供者</summary>
    public static IConfigProvider? Provider { get; set; }

    static Config()
    {
        // 创建提供者
        var att = typeof(TConfig).GetCustomAttribute<ConfigAttribute>(true);
        var value = att?.Name;
        if (value.IsNullOrEmpty())
        {
            value = typeof(TConfig).Name;
            if (value.EndsWith("Config") && value != "Config") value = value.TrimSuffix("Config");
            if (value.EndsWith("Setting") && value != "Setting") value = value.TrimSuffix("Setting");
        }
        var prv = ConfigProvider.Create(att?.Provider);
        if (prv is HttpConfigProvider _prv && att is HttpConfigAttribute _att)
        {
            _prv.Server = _att.Server;
            _prv.Action = _att.Action;
            _prv.AppId = _att.AppId;
            _prv.Secret = _att.Secret;
            _prv.Scope = _att.Scope;
            _prv.CacheLevel = _att.CacheLevel;
            _prv.Init(value);
        }
        else if (prv is ConfigProvider prv2)
        {
            prv2.Init(value);
        }

        Provider = prv;
    }

    private static volatile TConfig? _Current;
    /// <summary>当前实例。通过置空可以使其重新加载。</summary>
    public static TConfig Current
    {
        get
        {
            if (_Current != null) return _Current;
            lock (typeof(TConfig))
            {
                return _Current ??= Create();
            }
        }
        set { _Current = value; }
    }

    /// <summary>非阻塞读取当前实例。未初始化时返回空，且不触发初始化</summary>
    /// <remarks>
    /// 供程序集解析回调等不能执行任何初始化动作的上下文使用。这类回调可能在任意线程持有类初始化锁时被触发，
    /// 而配置初始化会绑定文件配置并创建定时器（需要类初始化与调度器监视器），形成互等死锁：
    /// Runtime 类初始化 → 程序集解析 → 配置初始化 → 定时调度器监视器 ↺ 定时器初始化 → Runtime 类初始化。
    /// 需要初始化时请走 <see cref="Current"/>。
    /// </remarks>
    /// <returns>已初始化的当前实例；未初始化时返回 null，绝不触发初始化</returns>
    internal static TConfig? TryGetCurrent() => _Current;

    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "配置绑定依赖类型成员反射；AOT 下请使用显式绑定入口")]
    private static TConfig Create()
    {
        var config = new TConfig();
        var prv = Provider ?? throw new ArgumentNullException(nameof(Provider));

        // 配置文件损坏时，要能够忽略错误，强行加载，避免影响系统正常运行
        try
        {
            // 绑定提供者数据到配置对象
            prv.Bind(config, true);
        }
        catch (Exception ex)
        {
            XTrace.Log?.Error(ex.Message);
        }

        try
        {
            config.OnLoaded();
        }
        catch (Exception ex)
        {
            XTrace.Log?.Error(ex.Message);
        }

        try
        {
            // OnLoad 中可能有变化，存回去
            //prv.Save(config);
            if (!prv.IsNew || Runtime.CreateConfigOnMissing) config.Save();
        }
        catch (Exception ex)
        {
            XTrace.Log?.Error(ex.Message);
        }

        return config;
    }
    #endregion

    #region 属性
    /// <summary>是否新的配置文件</summary>
    [XmlIgnore, IgnoreDataMember]
    //[Obsolete("=>_Provider.IsNew")]
    public Boolean IsNew => Provider?.IsNew ?? false;
    #endregion

    #region 成员方法
    /// <summary>从配置文件中读取完成后触发</summary>
    protected virtual void OnLoaded() { }

    /// <summary>保存到配置文件中去</summary>
    //[Obsolete("=>Provider.Save")]
    public virtual void Save() => Provider?.Save(this);
    #endregion
}