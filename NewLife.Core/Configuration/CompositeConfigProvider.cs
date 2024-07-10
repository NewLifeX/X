using System.Collections.Concurrent;

namespace NewLife.Configuration;

/// <summary>复合配置提供者。常用于本地配置与网络配置的混合</summary>
public class CompositeConfigProvider : IConfigProvider
{
    #region 属性
    /// <summary>日志提供者集合</summary>
    /// <remarks>为了线程安全，使用数组</remarks>
    public IConfigProvider[] Configs { get; set; } //= new IConfigProvider[0];

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>根元素</summary>
    public IConfigSection Root { get => Configs[0].Root; set => throw new NotImplementedException(); }

    /// <summary>所有键</summary>
    public ICollection<String> Keys
    {
        get
        {
            var ks = new List<String>();
            foreach (var cfg in Configs)
            {
                if (cfg.Keys != null)
                {
                    foreach (var item in cfg.Keys)
                    {
                        if (!ks.Contains(item)) ks.Add(item);
                    }
                }
            }

            return ks;
        }
    }

    /// <summary>是否新的配置文件</summary>
    public Boolean IsNew { get => Configs[0].IsNew; set => Configs[0].IsNew = value; }

    /// <summary>返回获取配置的委托</summary>
    public GetConfigCallback GetConfig => key => GetSection(key)?.Value;
    #endregion

    #region 构造
    ///// <summary>实例化</summary>
    //public CompositeConfigProvider() => Name = GetType().Name.TrimEnd("ConfigProvider");

    /// <summary>实例化</summary>
    /// <param name="configProvider1"></param>
    /// <param name="configProvider2"></param>
    public CompositeConfigProvider(IConfigProvider configProvider1, IConfigProvider configProvider2)
    {
        Name = GetType().Name.TrimEnd("ConfigProvider");

        Configs = [configProvider1, configProvider2];
    }

    /// <summary>添加</summary>
    /// <param name="configProviders"></param>
    public void Add(params IConfigProvider[] configProviders)
    {
        var list = new List<IConfigProvider>(Configs);
        list.AddRange(configProviders);

        Configs = list.ToArray();
    }
    #endregion

    #region 取值
    /// <summary>获取 或 设置 配置值</summary>
    /// <param name="key">键</param>
    /// <returns></returns>
    public String? this[String key]
    {
        get
        {
            foreach (var cfg in Configs)
            {
                var value = cfg[key];
                if (value != null) return value;
            }

            return null;
        }
        set
        {
            foreach (var cfg in Configs)
            {
                //cfg[key] = value;
                var section = cfg.GetSection(key);
                if (section != null) section.Value = value;
            }
        }
    }

    /// <summary>查找配置项。可得到子级和配置</summary>
    /// <param name="key">配置名</param>
    /// <returns></returns>
    public IConfigSection? GetSection(String key)
    {
        foreach (var cfg in Configs)
        {
            var section = cfg.GetSection(key);
            if (section != null) return section;
        }

        return null;
    }
    #endregion

    #region 方法
    /// <summary>从数据源加载数据到配置树</summary>
    public Boolean LoadAll()
    {
        var rs = false;
        foreach (var cfg in Configs)
        {
            rs |= cfg.LoadAll();
        }

        return rs;
    }

    /// <summary>保存配置树到数据源</summary>
    public Boolean SaveAll()
    {
        var rs = false;
        foreach (var cfg in Configs)
        {
            rs |= cfg.SaveAll();
        }

        return rs;
    }

    /// <summary>加载配置到模型</summary>
    /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
    /// <param name="path">路径。配置树位置，配置中心等多对象混合使用时</param>
    /// <returns></returns>
    public T? Load<T>(String? path = null) where T : new()
    {
        foreach (var cfg in Configs)
        {
            var model = cfg.Load<T>(path);
            if (model != null) return model;
        }

        return default;
    }

    /// <summary>保存模型实例</summary>
    /// <typeparam name="T">模型</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="path">路径。配置树位置</param>
    public Boolean Save<T>(T model, String? path = null)
    {
        foreach (var cfg in Configs)
        {
            var rs = cfg.Save(model, path);
            if (rs) return true;
        }

        return false;
    }
    #endregion

    #region 绑定
    private readonly ConcurrentDictionary<Object, String> _models = [];
    private readonly ConcurrentDictionary<Object, ModelWrap> _models2 = [];
    /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
    /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="autoReload">是否自动更新。默认true</param>
    /// <param name="path">命名空间。配置树位置，配置中心等多对象混合使用时</param>
    public virtual void Bind<T>(T model, Boolean autoReload = true, String? path = null)
    {
        if (model == null) return;

        // 如果有命名空间则使用指定层级数据源
        path ??= String.Empty;
        var source = GetSection(path);
        if (source != null)
        {
            if (model is IConfigMapping map)
                map.MapConfig(this, source);
            else
                source.MapTo(model, this);
        }

        if (autoReload)
        {
            _models.TryAdd(model, path);
        }

        AddChanged();
    }

    /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
    /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="path">命名空间。配置树位置，配置中心等多对象混合使用时</param>
    /// <param name="onChange">配置改变时执行的委托</param>
    public virtual void Bind<T>(T model, String? path, Action<IConfigSection> onChange)
    {
        if (model == null) return;

        // 如果有命名空间则使用指定层级数据源
        path ??= String.Empty;
        var source = GetSection(path);
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

        AddChanged();
    }

    private record ModelWrap(String Path, Action<IConfigSection> OnChange);

    /// <summary>通知绑定对象，配置数据有改变</summary>
    protected virtual void NotifyChange()
    {
        foreach (var item in _models)
        {
            var model = item.Key;
            var source = GetSection(item.Value);
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
            var model = item.Key;
            var source = GetSection(item.Value.Path);
            if (source != null) item.Value.OnChange(source);
        }

        // 通过事件通知外部
        _Changed?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region 配置变化
    private Int32 _count;

    private event EventHandler? _Changed;
    /// <summary>配置改变事件。执行了某些动作，可能导致配置数据发生改变时触发</summary>
    public event EventHandler Changed
    {
        add
        {
            _Changed += value;

            // 首次注册事件时，向内部提供者注册事件
            AddChanged();
        }
        remove
        {
            // 最后一次取消注册时，向内部提供者取消注册
            if (Interlocked.Decrement(ref _count) == 0)
            {
                foreach (var cfg in Configs)
                {
                    cfg.Changed -= OnChange;
                }
            }

            _Changed -= value;
        }
    }

    private void AddChanged()
    {
        if (Interlocked.Increment(ref _count) == 1)
        {
            foreach (var cfg in Configs)
            {
                cfg.Changed += OnChange;
            }
        }
    }

    private void OnChange(Object? sender, EventArgs e) => NotifyChange();
    #endregion
}