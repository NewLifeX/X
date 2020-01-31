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

        /// <summary>获取模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。映射时去掉</param>
        /// <returns>模型实例</returns>
        T Load<T>(String nameSpace = null) where T : new();

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。映射时加上</param>
        void Save<T>(T model, String nameSpace = null);

        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。映射时去掉</param>
        void Bind<T>(T model, String nameSpace = null);
    }

    /// <summary>配置提供者基类</summary>
    /// <remarks>
    /// 同时也是基于Items字典的内存配置提供者。
    /// </remarks>
    public class ConfigProvider : IConfigProvider
    {
        #region 属性
        private readonly Dictionary<String, ConfigItem> _Items = new Dictionary<String, ConfigItem>(StringComparer.OrdinalIgnoreCase);
        /// <summary>配置项集合</summary>
        public IDictionary<String, ConfigItem> Items => _Items;

        /// <summary>所有键</summary>
        public ICollection<String> Keys => Items.Keys;
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
        public virtual ConfigItem Find(String key, Boolean createOnMiss = false)
        {
            // 分层
            var ss = key.Split(':');

            // 顶级查找
            if (!_Items.TryGetValue(ss[0], out var config))
            {
                if (!createOnMiss) return null;

                config = new ConfigItem { Key = ss[0] };
                _Items[ss[0]] = config;
            }

            // 单层
            if (ss.Length <= 1) return config;

            // 逐级下钻
            for (var i = 1; i < ss.Length; i++)
            {
                var cfg = config.Childs?.FirstOrDefault(e => e.Key == ss[i]);
                if (cfg == null)
                {
                    if (!createOnMiss) return null;

                    cfg = new ConfigItem { Key = ss[i] };
                    if (config.Childs == null) config.Childs = new List<ConfigItem>();
                    config.Childs.Add(cfg);
                }

                config = cfg;
            }

            return config;
        }
        #endregion

        #region 加载/保存
        /// <summary>获取模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。映射时去掉</param>
        /// <returns>模型实例</returns>
        public virtual T Load<T>(String nameSpace = null) where T : new()
        {
            var model = new T();
            MapTo(Items, model, nameSpace);

            return model;
        }

        /// <summary>映射字典到公有实例属性</summary>
        /// <param name="source"></param>
        /// <param name="model"></param>
        /// <param name="nameSpace"></param>
        protected virtual void MapTo(IDictionary<String, ConfigItem> source, Object model, String nameSpace)
        {
            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.Name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

                var name = pi.Name;
                if (!nameSpace.IsNullOrEmpty()) name = $"{nameSpace}:{pi.Name}";

                // 分别处理基本类型和复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    if (source.TryGetValue(name, out var ci)) pi.SetValue(model, ci.Value.ChangeType(pi.PropertyType), null);
                }
                else
                {
                    // 复杂类型需要递归处理
                    var val = pi.GetValue(model, null);
                    if (val == null)
                    {
                        // 如果有无参构造函数，则实例化一个
                        var ci = pi.PropertyType.GetConstructor(new Type[0]);
                        if (ci != null)
                        {
                            val = ci.Invoke(null);
                            pi.SetValue(model, val, null);
                        }
                    }

                    // 递归映射
                    if (val != null) MapTo(source, val, name);
                }
            }
        }

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。映射时加上</param>
        public virtual void Save<T>(T model, String nameSpace = null) => MapFrom(Items, model, nameSpace);

        /// <summary>从公有实例属性映射到字典</summary>
        /// <param name="source"></param>
        /// <param name="model"></param>
        /// <param name="nameSpace"></param>
        protected virtual void MapFrom(IDictionary<String, ConfigItem> source, Object model, String nameSpace)
        {
            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.Name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

                // 名称前面加上命名空间
                var name = pi.Name;
                if (!nameSpace.IsNullOrEmpty()) name = $"{nameSpace}:{pi.Name}";

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
                    var str = "{0}".F(val);
                    if (source.TryGetValue(name, out var ci))
                    {
                        ci.Value = str;
                        ci.Description = remark;
                    }
                    else
                        source.Add(name, new ConfigItem { Key = name, Value = str, Description = remark });
                }
                else
                {
                    // 递归映射
                    if (val != null) MapFrom(source, val, name);
                }
            }
        }

        /// <summary>合并源字典到目标字典</summary>
        /// <param name="source">源字典</param>
        /// <param name="dest">目标字典</param>
        /// <param name="nameSpace">命名空间</param>
        protected virtual void Merge(IDictionary<String, ConfigItem> source, IDictionary<String, ConfigItem> dest, String nameSpace)
        {
            foreach (var item in source)
            {
                var name = item.Key;
                if (!nameSpace.IsNullOrEmpty()) name = $"{nameSpace}:{item.Key}";

                dest[name] = item.Value;
            }
        }
        #endregion

        #region 绑定
        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。映射时去掉</param>
        public virtual void Bind<T>(T model, String nameSpace = null) => MapTo(Items, model, nameSpace);
        #endregion
    }

    /// <summary>配置项</summary>
    public class ConfigItem
    {
        /// <summary>配置名</summary>
        public String Key { get; set; }

        /// <summary>配置值</summary>
        public String Value { get; set; }

        /// <summary>注释</summary>
        public String Description { get; set; }

        /// <summary>子级</summary>
        public IList<ConfigItem> Childs { get; set; }
    }
}