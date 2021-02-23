using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Reflection;

namespace NewLife.Configuration
{
    /// <summary>配置提供者</summary>
    /// <remarks>
    /// 建立树状配置数据体系，以分布式配置中心为核心，支持基于key的索引读写，也支持Load/Save/Bind的实体模型转换。
    /// key索引支持冒号分隔的多层结构，在配置中心中不同命名空间使用不同提供者实例，在文件配置中不同文件使用不同提供者实例。
    /// 
    /// 一个配置类，支持从不同持久化提供者读取，可根据需要选择配置持久化策略。
    /// 例如，小系统采用ini/xml/json文件配置，分布式系统采用配置中心。
    /// 
    /// 可通过实现IConfigMapping接口来自定义映射配置到模型实例。
    /// </remarks>
    public interface IConfigProvider
    {
        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>所有键</summary>
        ICollection<String> Keys { get; }

        /// <summary>获取 或 设置 配置值</summary>
        /// <param name="key">配置名，支持冒号分隔的多级名称</param>
        /// <returns></returns>
        String this[String key] { get; set; }

        /// <summary>查找配置项。可得到子级和配置</summary>
        /// <param name="key">配置名</param>
        /// <returns></returns>
        IConfigSection GetSection(String key);

        /// <summary>返回获取配置的委托</summary>
        GetConfigCallback GetConfig { get; }

        /// <summary>加载配置到模型</summary>
        /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
        /// <param name="path">路径。配置树位置，配置中心等多对象混合使用时</param>
        /// <returns></returns>
        T Load<T>(String path = null) where T : new();

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="path">路径。配置树位置，配置中心等多对象混合使用时</param>
        Boolean Save<T>(T model, String path = null);

        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="autoReload">是否自动更新。默认true</param>
        /// <param name="path">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        void Bind<T>(T model, Boolean autoReload = true, String path = null);
    }

    /// <summary>配置提供者基类</summary>
    /// <remarks>
    /// 同时也是基于Items字典的内存配置提供者。
    /// </remarks>
    public class ConfigProvider : DisposeBase, IConfigProvider
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>根元素</summary>
        public IConfigSection Root { get; protected set; } = new ConfigSection { Childs = new List<IConfigSection>() };

        /// <summary>所有键</summary>
        public ICollection<String> Keys => Root.Childs.Select(e => e.Key).ToList();
        #endregion

        #region 构造
        /// <summary>构造函数</summary>
        public ConfigProvider() => Name = GetType().Name.TrimEnd("ConfigProvider");
        #endregion

        #region 方法
        /// <summary>获取 或 设置 配置值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public virtual String this[String key]
        {
            get { EnsureLoad(); return Root.Find(key, false)?.Value; }
            set => Root.Find(key, true).Value = value;
        }

        /// <summary>查找配置项。可得到子级和配置</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual IConfigSection GetSection(String key) => Root.Find(key, false);

        /// <summary>返回获取配置的委托</summary>
        public virtual GetConfigCallback GetConfig => key => Root.Find(key, false)?.Value;

        /// <summary>初始化提供者</summary>
        /// <param name="value"></param>
        public virtual void Init(String value) { }
        #endregion

        #region 加载/保存
        /// <summary>从数据源加载数据到配置树</summary>
        public virtual Boolean LoadAll() => true;

        private Boolean _Loaded;
        private void EnsureLoad()
        {
            if (_Loaded) return;

            LoadAll();

            _Loaded = true;
        }

        /// <summary>加载配置到模型</summary>
        /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
        /// <param name="path">路径。配置树位置，配置中心等多对象混合使用时</param>
        /// <returns></returns>
        public virtual T Load<T>(String path = null) where T : new()
        {
            EnsureLoad();

            // 如果有命名空间则使用指定层级数据源
            var source = GetSection(path);
            if (source == null) return default;

            var model = new T();
            if (model is IConfigMapping map)
                map.MapConfig(this, source);
            else
                source.MapTo(model);

            return model;
        }

        /// <summary>保存配置树到数据源</summary>
        public virtual Boolean SaveAll() => true;

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="path">路径。配置树位置</param>
        public virtual Boolean Save<T>(T model, String path = null)
        {
            EnsureLoad();

            // 如果有命名空间则使用指定层级数据源
            var source = GetSection(path);
            source?.MapFrom(model);

            return SaveAll();
        }
        #endregion

        #region 绑定
        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型。可通过实现IConfigMapping接口来自定义映射配置到模型实例</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="autoReload">是否自动更新。默认true</param>
        /// <param name="path">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        public virtual void Bind<T>(T model, Boolean autoReload = true, String path = null)
        {
            EnsureLoad();

            // 如果有命名空间则使用指定层级数据源
            var source = GetSection(path);
            if (source != null)
            {
                if (model is IConfigMapping map)
                    map.MapConfig(this, source);
                else
                    source.MapTo(model);
            }
        }
        #endregion

        #region 静态
        /// <summary>默认提供者。默认xml</summary>
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
            if (str.IsNullOrEmpty()) str = Environment.GetEnvironmentVariable("DefaultConfig");
            if (!str.IsNullOrEmpty()) DefaultProvider = str;

            Register<InIConfigProvider>("ini");
            Register<XmlConfigProvider>("xml");
            Register<JsonConfigProvider>("json");
            Register<HttpConfigProvider>("http");

            Register<XmlConfigProvider>("config");
        }

        private static readonly IDictionary<String, Type> _providers = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
        /// <summary>注册提供者</summary>
        /// <typeparam name="TProvider"></typeparam>
        /// <param name="name"></param>
        public static void Register<TProvider>(String name) where TProvider : IConfigProvider, new() => _providers[name] = typeof(TProvider);

        /// <summary>根据指定名称创建提供者</summary>
        /// <remarks>
        /// 如果是文件名，根据后缀确定使用哪一种提供者。
        /// </remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IConfigProvider Create(String name)
        {
            if (name.IsNullOrEmpty()) name = DefaultProvider;

            var p = name.LastIndexOf('.');
            var ext = p >= 0 ? name.Substring(p + 1) : name;
            if (!_providers.TryGetValue(ext, out _)) ext = DefaultProvider;
            if (!_providers.TryGetValue(ext, out var type)) throw new Exception($"无法为[{name}]找到适配的配置提供者！");

            var config = type.CreateInstance() as IConfigProvider;

            return config;
        }
        #endregion
    }
}