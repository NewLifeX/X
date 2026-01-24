using System.Collections.Concurrent;
using NewLife.Reflection;

namespace NewLife.Configuration;

/// <summary>配置提供者</summary>
/// <remarks>
/// 建立树状配置数据体系，以分布式配置中心为核心：
/// - 支持以“冒号分隔”的层级 Key 索引读写
/// - 支持 Load/Save/Bind 将配置与实体模型互相映射
/// - 配置中心可用不同命名空间实例隔离；文件配置可用不同文件实例隔离
/// 可通过实现 IConfigMapping 来自定义映射策略。
/// </remarks>
public interface IConfigProvider
{
    /// <summary>名称</summary>
    String Name { get; set; }

    /// <summary>根元素</summary>
    IConfigSection Root { get; set; }

    /// <summary>所有键</summary>
    /// <remarks>
    /// 返回当前“根级”直接子节点的键集合（不保证包含深层键），主要用于枚举或提示。
    /// 具体实现可按需覆盖，返回包含深层键的集合。
    /// </remarks>
    ICollection<String> Keys { get; }

    /// <summary>是否新的配置文件</summary>
    /// <remarks>true 表示数据源尚不存在或刚创建；用于决定是否在首次加载后持久化默认值。</remarks>
    Boolean IsNew { get; set; }

    /// <summary>获取/设置配置值</summary>
    /// <param name="key">配置名，支持冒号分隔的多级名称</param>
    /// <returns>找到时返回配置值；未找到返回 null</returns>
    String? this[String key] { get; set; }

    /// <summary>查找配置项（返回节点对象，可获取子级与值）</summary>
    /// <param name="key">配置名，支持冒号分隔的多级名称</param>
    /// <returns>匹配的配置节；未找到时返回 null</returns>
    IConfigSection? GetSection(String key);

    /// <summary>配置改变事件</summary>
    /// <remarks>调用 Save/SaveAll、远端推送/轮询变更、文件变更等场景会触发。订阅方应避免在回调中执行耗时逻辑。</remarks>
    event EventHandler? Changed;

    /// <summary>返回获取配置的委托</summary>
    GetConfigCallback GetConfig { get; }

    /// <summary>从数据源加载数据到配置树</summary>
    /// <returns>true 表示加载成功；false 表示被忽略或失败</returns>
    Boolean LoadAll();

    /// <summary>保存配置树到数据源</summary>
    /// <returns>true 表示保存成功；false 表示被忽略或失败</returns>
    Boolean SaveAll();

    /// <summary>加载配置到模型</summary>
    /// <typeparam name="T">模型类型。可通过实现 IConfigMapping 自定义映射</typeparam>
    /// <param name="path">路径/命名空间。配置树位置，配置中心等多对象混合使用时</param>
    /// <returns>模型实例；未找到对应配置时返回默认实例或 null（由实现决定）</returns>
    T? Load<T>(String? path = null) where T : new();

    /// <summary>保存模型实例</summary>
    /// <typeparam name="T">模型类型</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="path">路径/命名空间。配置树位置，配置中心等多对象混合使用时</param>
    /// <returns>true 表示保存成功；false 表示失败</returns>
    Boolean Save<T>(T model, String? path = null);

    /// <summary>绑定模型以实现热更新：配置数据变化时同步修改模型属性</summary>
    /// <typeparam name="T">模型类型。可通过实现 IConfigMapping 自定义映射</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="autoReload">是否自动更新（默认 true）</param>
    /// <param name="path">路径/命名空间。配置树位置，配置中心等多对象混合使用时</param>
    void Bind<T>(T model, Boolean autoReload = true, String? path = null);

    /// <summary>绑定模型以实现热更新：配置变化时回调自定义逻辑</summary>
    /// <typeparam name="T">模型类型。可通过实现 IConfigMapping 自定义映射</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="path">路径/命名空间。配置树位置，配置中心等多对象混合使用时</param>
    /// <param name="onChange">配置改变时执行的委托</param>
    void Bind<T>(T model, String path, Action<IConfigSection> onChange);
}

/// <summary>配置提供者基类</summary>
/// <remarks>同时也是基于 Items 字典的内存配置提供者。</remarks>
public abstract class ConfigProvider : DisposeBase, IConfigProvider
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>根元素</summary>
    public virtual IConfigSection Root { get; set; } = new ConfigSection { Childs = [] };

    /// <summary>所有键</summary>
    public virtual ICollection<String> Keys
    {
        get
        {
            // 确保已加载
            EnsureLoad();

            var childs = Root?.Childs;
            if (childs == null || childs.Count == 0) return [];

            var list = new List<String>(childs.Count);
            foreach (var item in childs)
            {
                if (item.Key != null) list.Add(item.Key);
            }

            return list;
        }
    }

    /// <summary>已使用的键（一次会话内访问过的键）</summary>
    public ICollection<String> UsedKeys { get; } = new HashSet<String>();

    /// <summary>缺失的键（访问时未命中的键）</summary>
    public ICollection<String> MissedKeys { get; } = new HashSet<String>();

    /// <summary>返回获取配置的委托</summary>
    public virtual GetConfigCallback GetConfig => key => Find(key, false)?.Value;

    /// <summary>配置改变事件。执行了某些动作，可能导致配置数据发生改变时触发</summary>
    public event EventHandler? Changed;

    /// <summary>是否新的配置文件</summary>
    public Boolean IsNew { get; set; }
    #endregion

    #region 构造
    /// <summary>构造函数</summary>
    public ConfigProvider() => Name = GetType().Name.TrimEnd("ConfigProvider");
    #endregion

    #region 方法
    /// <summary>获取或设置配置值</summary>
    /// <param name="key">键</param>
    public virtual String? this[String key]
    {
        get { EnsureLoad(); return Find(key, false)?.Value; }
        set
        {
            var section = Find(key, true);
            section?.Value = value;
        }
    }

    /// <summary>查找配置项。可得到子级和配置</summary>
    /// <param name="key">键</param>
    public virtual IConfigSection? GetSection(String key) => Find(key, false);

    /// <summary>查找配置项，可指定是否创建</summary>
    /// <remarks>配置提供者可以重载该方法以增强功能，例如：从注册中心或远端配置中心读取数据。</remarks>
    /// <param name="key">键</param>
    /// <param name="createOnMiss">未找到时是否创建</param>
    protected virtual IConfigSection? Find(String key, Boolean createOnMiss)
    {
        UseKey(key);

        EnsureLoad();

        var sec = Root.Find(key, createOnMiss);
        if (sec == null) MissKey(key);

        return sec;
    }

    internal void UseKey(String key)
    {
        if (!key.IsNullOrEmpty() && !UsedKeys.Contains(key)) UsedKeys.Add(key);
    }

    internal void MissKey(String key)
    {
        if (!key.IsNullOrEmpty() && !MissedKeys.Contains(key)) MissedKeys.Add(key);
    }

    /// <summary>初始化提供者</summary>
    /// <param name="value">初始化参数</param>
    public virtual void Init(String value) { }
    #endregion

    #region 加载/保存
    /// <summary>从数据源加载数据到配置树</summary>
    public virtual Boolean LoadAll() => true;

    private volatile Boolean _loaded;
    private readonly Object _syncRoot = new();

    private void EnsureLoad()
    {
        if (_loaded) return;
        lock (_syncRoot)
        {
            if (_loaded) return;

            LoadAll();

            _loaded = true;
        }
    }

    /// <summary>加载配置到模型</summary>
    /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
    /// <param name="path">路径。配置树位置，配置中心等多对象混合使用时</param>
    public virtual T? Load<T>(String? path = null) where T : new()
    {
        EnsureLoad();

        // 如果有命名空间则使用指定层级数据源
        var source = path.IsNullOrEmpty() ? Root : GetSection(path);
        if (source == null) return default;

        var model = new T();
        if (model is IConfigMapping map)
            map.MapConfig(this, source);
        else
            source.MapTo(model, this);

        return model;
    }

    /// <summary>保存配置树到数据源</summary>
    public virtual Boolean SaveAll()
    {
        NotifyChange();

        return true;
    }

    /// <summary>保存模型实例</summary>
    /// <typeparam name="T">模型</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="path">路径。配置树位置</param>
    public virtual Boolean Save<T>(T model, String? path = null)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        EnsureLoad();

        // 如果有命名空间则使用指定层级数据源
        var source = path.IsNullOrEmpty() ? Root : Find(path, true);
        source?.MapFrom(model);

        return SaveAll();
    }
    #endregion

    #region 绑定
    private readonly ConcurrentDictionary<Object, String> _models = new();
    private readonly ConcurrentDictionary<Object, ModelWrap> _models2 = new();

    /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
    /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="autoReload">是否自动更新。默认true</param>
    /// <param name="path">命名空间。配置树位置，配置中心等多对象混合使用时</param>
    public virtual void Bind<T>(T model, Boolean autoReload = true, String? path = null)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        EnsureLoad();

        // 如果有命名空间则使用指定层级数据源
        var source = path.IsNullOrEmpty() ? Root : GetSection(path);
        if (source != null)
        {
            if (model is IConfigMapping map)
                map.MapConfig(this, source);
            else
                source.MapTo(model, this);
        }

        if (autoReload)
        {
            path ??= String.Empty;
            _models.TryAdd(model, path);
        }
    }

    /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
    /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="path">命名空间。配置树位置，配置中心等多对象混合使用时</param>
    /// <param name="onChange">配置改变时执行的委托</param>
    public virtual void Bind<T>(T model, String path, Action<IConfigSection> onChange)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        EnsureLoad();

        // 如果有命名空间则使用指定层级数据源
        var source = path.IsNullOrEmpty() ? Root : GetSection(path);
        if (source != null)
        {
            if (model is IConfigMapping map)
                map.MapConfig(this, source);
            else
                source.MapTo(model, this);
        }

        if (onChange != null)
        {
            _models2.TryAdd(model, new ModelWrap(path, onChange));
        }
    }

    private record ModelWrap(String Path, Action<IConfigSection> OnChange);

    /// <summary>通知绑定对象，配置数据有改变</summary>
    protected virtual void NotifyChange()
    {
        foreach (var item in _models)
        {
            var model = item.Key;
            var path = item.Value;
            var source = path.IsNullOrEmpty() ? Root : GetSection(path);
            if (source != null)
            {
                if (model is IConfigMapping map)
                    map.MapConfig(this, source);
                else
                    source.MapTo(model, this);
            }
        }
        foreach (var item in _models2)
        {
            var path = item.Value.Path;
            var source = path.IsNullOrEmpty() ? Root : GetSection(path);
            if (source != null) item.Value.OnChange(source);
        }

        // 通过事件通知外部
        Changed?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region 静态
    /// <summary>默认提供者。默认 xml</summary>
    public static String DefaultProvider { get; set; } = "xml";

    static ConfigProvider()
    {
        // 支持从命令行参数和环境变量设定默认配置提供者
        var str = "";
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].EqualIgnoreCase("-DefaultConfig", "--DefaultConfig") && i + 1 < args.Length)
            {
                str = args[i + 1];
                break;
            }
        }
        if (str.IsNullOrEmpty()) str = NewLife.Runtime.GetEnvironmentVariable("DefaultConfig");
        if (!str.IsNullOrEmpty()) DefaultProvider = str;

        Register<IniConfigProvider>("ini");
        Register<XmlConfigProvider>("xml");
        Register<JsonConfigProvider>("json");
        Register<HttpConfigProvider>("http");

        Register<XmlConfigProvider>("config");
    }

    private static readonly ConcurrentDictionary<String, Type> _providers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>注册提供者</summary>
    /// <typeparam name="TProvider">实现 IConfigProvider 的类型</typeparam>
    /// <param name="name">提供者名称或文件后缀（不含点）</param>
    public static void Register<TProvider>(String name) where TProvider : IConfigProvider, new()
    {
        if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));
        _providers[name] = typeof(TProvider);
    }

    /// <summary>根据指定名称创建提供者</summary>
    /// <remarks>传入文件名则按扩展名选择提供者；否则按名称选择。</remarks>
    /// <param name="name">名称或文件名；null/空时使用 <see cref="DefaultProvider"/></param>
    public static IConfigProvider? Create(String? name)
    {
        if (name.IsNullOrEmpty()) name = DefaultProvider;

        var p = name.LastIndexOf('.');
        var ext = p >= 0 ? name[(p + 1)..] : name;
        if (!_providers.TryGetValue(ext, out _)) ext = DefaultProvider;
        if (!_providers.TryGetValue(ext, out var type)) throw new Exception($"Unable to find an appropriate configuration provider for [{name}]");

        var config = type.CreateInstance() as IConfigProvider;

        return config;
    }
    #endregion
}