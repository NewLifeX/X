using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.Configuration
{
    /// <summary>配置提供者</summary>
    /// <remarks>
    /// 建立扁平化配置数据体系，以分布式配置中心为核心，支持基于key的索引读写，也支持Load/Save/Bind的实体模型转换。
    /// key索引支持冒号分隔的多层结构，在配置中心中作为整个key存在，在文件配置中第一段表示不同文件。
    /// </remarks>
    public interface IConfigProvider
    {
        /// <summary>所有键</summary>
        ICollection<String> Keys { get; }

        /// <summary>获取 或 设置 配置值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        String this[String key] { get; set; }

        /// <summary>从数据源加载数据到配置树</summary>
        void LoadAll();

        /// <summary>保存配置树到数据源</summary>
        void SaveAll();

        /// <summary>加载配置到模型</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        /// <returns></returns>
        T Load<T>(String nameSpace = null) where T : new();

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        void Save<T>(T model, String nameSpace = null);

        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="autoReload">是否自动更新。默认true</param>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        void Bind<T>(T model, Boolean autoReload = true, String nameSpace = null);
    }

    ///// <summary>配置助手</summary>
    //public static class ConfigHelper
    //{
    //    /// <summary>加载配置到模型</summary>
    //    /// <typeparam name="T">模型</typeparam>
    //    /// <param name="provider"></param>
    //    /// <returns></returns>
    //    public static T Load<T>(this IConfigProvider provider) where T : new()
    //    {
    //        if (provider is FileConfigProvider fcp && fcp.ModelType == null) fcp.ModelType = typeof(T);

    //        var model = new T();
    //        provider.Bind(model, true);

    //        return model;
    //    }

    //    /// <summary>保存模型实例</summary>
    //    /// <typeparam name="T">模型</typeparam>
    //    /// <param name="provider"></param>
    //    /// <param name="model">模型实例</param>
    //    public static void Save<T>(this IConfigProvider provider, T model)
    //    {
    //        provider.Save();
    //    }
    //}

    /// <summary>配置提供者基类</summary>
    /// <remarks>
    /// 同时也是基于Items字典的内存配置提供者。
    /// </remarks>
    public class ConfigProvider : IConfigProvider
    {
        #region 属性
        private readonly IConfigSection _Root = new ConfigSection { Childs = new List<IConfigSection>() };
        /// <summary>根元素</summary>
        public IConfigSection Root => _Root;

        /// <summary>所有键</summary>
        public ICollection<String> Keys => _Root.Childs.Select(e => e.Key).ToList();
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
        /// <param name="createOnMiss"></param>
        /// <returns></returns>
        public virtual IConfigSection Find(String key, Boolean createOnMiss = false)
        {
            // 分层
            var ss = key.Split(':');

            var config = _Root;

            // 逐级下钻
            for (var i = 0; i < ss.Length; i++)
            {
                var cfg = config.Childs?.FirstOrDefault(e => e.Key == ss[i]);
                if (cfg == null)
                {
                    if (!createOnMiss) return null;

                    cfg = new ConfigSection { Key = ss[i] };
                    if (config.Childs == null) config.Childs = new List<IConfigSection>();
                    config.Childs.Add(cfg);
                }

                config = cfg;
            }

            return config;
        }
        #endregion

        #region 加载/保存
        /// <summary>加载配置</summary>
        public virtual void LoadAll() { }

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
            var source = nameSpace.IsNullOrEmpty() ? _Root : Find(nameSpace);
            if (source == null) return default;

            var model = new T();
            MapTo(source, model);

            return model;
        }

        /// <summary>映射字典到公有实例属性</summary>
        /// <param name="source">数据源</param>
        /// <param name="model">模型</param>
        protected virtual void MapTo(IConfigSection source, Object model)
        {
            if (source == null || source.Childs == null || source.Childs.Count == 0) return;

            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.Name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

                var name = pi.Name;
                var cfg = source.Childs.FirstOrDefault(e => e.Key == name);
                if (cfg == null) continue;

                // 分别处理基本类型和复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    pi.SetValue(model, cfg.Value.ChangeType(pi.PropertyType), null);
                }
                else if (cfg.Childs != null)
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

        /// <summary>保存配置树到数据源</summary>
        public virtual void SaveAll() { }

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。配置树位置</param>
        public virtual void Save<T>(T model, String nameSpace = null)
        {
            // 如果有命名空间则使用指定层级数据源
            var source = nameSpace.IsNullOrEmpty() ? _Root : Find(nameSpace);
            if (source != null) MapFrom(source, model);

            SaveAll();
        }

        /// <summary>从公有实例属性映射到字典</summary>
        /// <param name="source"></param>
        /// <param name="model"></param>
        /// <param name="nameSpace"></param>
        protected virtual void MapFrom(IConfigSection source, Object model)
        {
            if (source == null) return;

            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.Name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

                // 名称前面加上命名空间
                var name = pi.Name;
                var cfg = source.Childs.FirstOrDefault(e => e.Key == name);
                if (cfg == null)
                {
                    cfg = new ConfigSection { Key = name };
                    source.Childs.Add(cfg);
                }

                // 反射获取属性值
                var val = pi.GetValue(model, null);
                var att = pi.GetCustomAttribute<DescriptionAttribute>();
                var remark = att?.Description;
                if (remark.IsNullOrEmpty())
                {
                    var att2 = pi.GetCustomAttribute<DisplayNameAttribute>();
                    remark = att2?.DisplayName;
                }

                // 分别处理基本类型和复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    // 格式化为字符串，主要处理时间日期格式
                    cfg.Value = "{0}".F(val);
                    cfg.Description = remark;
                }
                else
                {
                    // 递归映射
                    if (val != null) MapFrom(cfg, val);
                }
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
            var source = nameSpace.IsNullOrEmpty() ? _Root : Find(nameSpace);
            if (source != null) MapTo(source, model);
        }
        #endregion
    }
}