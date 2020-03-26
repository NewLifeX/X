using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Reflection;

namespace NewLife.Configuration
{
    /// <summary>配置提供者</summary>
    /// <remarks>
    /// 建立树状配置数据体系，以分布式配置中心为核心，支持基于key的索引读写，也支持Load/Save/Bind的实体模型转换。
    /// key索引支持冒号分隔的多层结构，在配置中心中作为整个key存在，在文件配置中第一段表示不同文件。
    /// 
    /// 一个配置类，支持从不同持久化提供者读取，可根据需要选择配置持久化策略。
    /// 例如，小系统采用ini/xml/json文件配置，分布式系统采用配置中心。
    /// </remarks>
    public interface IConfigProvider
    {
        /// <summary>所有键</summary>
        ICollection<String> Keys { get; }

        /// <summary>获取 或 设置 配置值</summary>
        /// <param name="key">配置名</param>
        /// <returns></returns>
        String this[String key] { get; set; }

        /// <summary>查找配置项。可得到子级和配置</summary>
        /// <param name="key">配置名</param>
        /// <returns></returns>
        IConfigSection GetSection(String key);

        /// <summary>从数据源加载数据到配置树</summary>
        Boolean LoadAll();

        /// <summary>保存配置树到数据源</summary>
        Boolean SaveAll();

        /// <summary>加载配置到模型</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        /// <returns></returns>
        T Load<T>(String nameSpace = null) where T : new();

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        Boolean Save<T>(T model, String nameSpace = null);

        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="autoReload">是否自动更新。默认true</param>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        void Bind<T>(T model, Boolean autoReload = true, String nameSpace = null);
    }

    /// <summary>配置助手</summary>
    public static class ConfigHelper
    {
        /// <summary>添加子节点</summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IConfigSection AddChild(this IConfigSection section, String key)
        {
            if (section == null) return null;

            var cfg = new ConfigSection { Key = key };
            if (section.Childs == null) section.Childs = new List<IConfigSection>();
            section.Childs.Add(cfg);

            return cfg;
        }

        /// <summary>查找或添加子节点</summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IConfigSection GetOrAddChild(this IConfigSection section, String key)
        {
            if (section == null) return null;

            var cfg = section.Childs?.FirstOrDefault(e => e.Key == key);
            if (cfg != null) return cfg;

            cfg = new ConfigSection { Key = key };
            if (section.Childs == null) section.Childs = new List<IConfigSection>();
            section.Childs.Add(cfg);

            return cfg;
        }

        internal static void SetValue(this IConfigSection section, Object value)
        {
            if (value is DateTime dt)
                section.Value = dt.ToFullString();
            else if (value is Boolean b)
                section.Value = b.ToString().ToLower();
            else
                section.Value = value?.ToString();
        }
    }

    /// <summary>配置提供者基类</summary>
    /// <remarks>
    /// 同时也是基于Items字典的内存配置提供者。
    /// </remarks>
    public class ConfigProvider : IConfigProvider
    {
        #region 属性
        /// <summary>根元素</summary>
        public IConfigSection Root { get; protected set; } = new ConfigSection { Childs = new List<IConfigSection>() };

        /// <summary>所有键</summary>
        public ICollection<String> Keys => Root.Childs.Select(e => e.Key).ToList();
        #endregion

        #region 方法
        /// <summary>获取 或 设置 配置值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public virtual String this[String key]
        {
            get => Find(key, false)?.Value;
            set => Find(key, true).Value = value;
        }

        /// <summary>查找配置项。可得到子级和配置</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual IConfigSection GetSection(String key) => Find(key, false);

        /// <summary>查找配置项。可得到子级和配置</summary>
        /// <param name="key"></param>
        /// <param name="createOnMiss"></param>
        /// <returns></returns>
        protected virtual IConfigSection Find(String key, Boolean createOnMiss = false)
        {
            if (key.IsNullOrEmpty()) return Root;

            // 分层
            var ss = key.Split(':');

            var section = Root;

            // 逐级下钻
            for (var i = 0; i < ss.Length; i++)
            {
                var cfg = section.Childs?.FirstOrDefault(e => e.Key == ss[i]);
                if (cfg == null)
                {
                    if (!createOnMiss) return null;

                    cfg = section.AddChild(ss[i]);
                }

                section = cfg;
            }

            return section;
        }

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
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        /// <returns></returns>
        public virtual T Load<T>(String nameSpace = null) where T : new()
        {
            EnsureLoad();

            // 如果有命名空间则使用指定层级数据源
            var source = GetSection(nameSpace);
            if (source == null) return default;

            var model = new T();
            MapTo(source, model);

            return model;
        }

        /// <summary>映射配置树到实例公有属性</summary>
        /// <param name="section">数据源</param>
        /// <param name="model">模型</param>
        protected virtual void MapTo(IConfigSection section, Object model)
        {
            if (section == null || section.Childs == null || section.Childs.Count == 0) return;

            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
                if (pi.Name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

                var name = pi.Name;
                var cfg = section.Childs?.FirstOrDefault(e => e.Key == name);
                if (cfg == null) continue;

                // 分别处理基本类型、数组类型、复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    pi.SetValue(model, cfg.Value.ChangeType(pi.PropertyType), null);
                }
                else if (cfg.Childs != null)
                {
                    if (pi.PropertyType.As<IList>())
                    {
                        if (pi.PropertyType.IsArray)
                            MapArray(cfg, model, pi);
                        else
                            MapList(cfg, model, pi);
                    }
                    else
                    {
                        // 复杂类型需要递归处理
                        var val = pi.GetValue(model, null);
                        if (val == null)
                        {
                            // 如果有无参构造函数，则实例化一个
                            var ctor = pi.PropertyType.GetConstructor(new Type[0]);
                            if (ctor != null)
                            {
                                val = ctor.Invoke(null);
                                pi.SetValue(model, val, null);
                            }
                        }

                        // 递归映射
                        if (val != null) MapTo(cfg, val);
                    }
                }
            }
        }

        private void MapArray(IConfigSection section, Object model, PropertyInfo pi)
        {
            var elementType = pi.PropertyType.GetElementTypeEx();

            // 实例化数组
            var arr = pi.GetValue(model, null) as Array;
            if (arr == null)
            {
                arr = Array.CreateInstance(elementType, section.Childs.Count);
                pi.SetValue(model, arr, null);
            }

            // 逐个映射
            for (var i = 0; i < section.Childs.Count; i++)
            {
                var val = elementType.CreateInstance();
                MapTo(section.Childs[i], val);
                arr.SetValue(val, i);
            }
        }

        private void MapList(IConfigSection section, Object model, PropertyInfo pi)
        {
            var elementType = pi.PropertyType.GetElementTypeEx();

            // 实例化列表
            var list = pi.GetValue(model, null) as IList;
            if (list == null)
            {
                var obj = !pi.PropertyType.IsInterface ?
                    pi.PropertyType.CreateInstance() :
                    typeof(List<>).MakeGenericType(elementType).CreateInstance();

                list = obj as IList;
                if (list == null) return;

                pi.SetValue(model, list, null);
            }

            // 逐个映射
            for (var i = 0; i < section.Childs.Count; i++)
            {
                var val = elementType.CreateInstance();
                MapTo(section.Childs[i], val);
                list[i] = val;
            }
        }

        /// <summary>保存配置树到数据源</summary>
        public virtual Boolean SaveAll() => true;

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。配置树位置</param>
        public virtual Boolean Save<T>(T model, String nameSpace = null)
        {
            // 如果有命名空间则使用指定层级数据源
            var source = GetSection(nameSpace);
            if (source != null) MapFrom(source, model);

            return SaveAll();
        }

        /// <summary>从实例公有属性映射到配置树</summary>
        /// <param name="section"></param>
        /// <param name="model"></param>
        protected virtual void MapFrom(IConfigSection section, Object model)
        {
            if (section == null) return;

            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
                if (pi.Name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

                // 名称前面加上命名空间
                var name = pi.Name;
                var cfg = section.GetOrAddChild(name);

                // 反射获取属性值
                var val = pi.GetValue(model, null);
                var att = pi.GetCustomAttribute<DescriptionAttribute>();
                cfg.Comment = att?.Description;
                if (cfg.Comment.IsNullOrEmpty())
                {
                    var att2 = pi.GetCustomAttribute<DisplayNameAttribute>();
                    cfg.Comment = att2?.DisplayName;
                }

                if (val == null) continue;

                // 分别处理基本类型、数组类型、复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    cfg.SetValue(val);
                }
                else if (pi.PropertyType.As<IList>())
                {
                    if (val is IList list) MapArray(section, cfg, list, pi.PropertyType.GetElementTypeEx());
                }
                else
                {
                    // 递归映射
                    MapFrom(cfg, val);
                }
            }
        }

        private void MapArray(IConfigSection section, IConfigSection cfg, IList list, Type elementType)
        {
            // 为了避免数组元素叠加，干掉原来的
            section.Childs.Remove(cfg);
            cfg = new ConfigSection { Key = cfg.Key, Childs = new List<IConfigSection>(), Comment = cfg.Comment };
            section.Childs.Add(cfg);

            // 数组元素是没有key的集合
            foreach (var item in list)
            {
                if (item == null) continue;

                var cfg2 = cfg.AddChild(elementType.Name);

                // 分别处理基本类型和复杂类型
                if (item.GetType().GetTypeCode() != TypeCode.Object)
                    cfg2.SetValue(item);
                else
                    MapFrom(cfg2, item);
            }
        }
        #endregion

        #region 绑定
        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="autoReload">是否自动更新。默认true</param>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        public virtual void Bind<T>(T model, Boolean autoReload = true, String nameSpace = null)
        {
            // 如果有命名空间则使用指定层级数据源
            var source = GetSection(nameSpace);
            if (source != null) MapTo(source, model);
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

            if (!_providers.TryGetValue(ext, out var type)) ext = DefaultProvider;
            if (!_providers.TryGetValue(ext, out type)) throw new Exception($"无法为[{name}]找到适配的配置提供者！");

            var config = type.CreateInstance() as IConfigProvider;

            return config;
        }
        #endregion
    }
}