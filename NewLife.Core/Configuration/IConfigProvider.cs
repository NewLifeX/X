using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Collections;
using NewLife.Json;
using NewLife.Reflection;
using NewLife.Xml;

namespace NewLife.Configuration
{
    /// <summary>配置提供者</summary>
    /// <remarks>
    /// 建立扁平化配置数据体系，以分布式配置中心为核心，支持基于key的索引读写，也支持Load/Save/Bind的实体模型转换。
    /// key索引支持冒号分隔的多层结构，在配置中心中作为整个key存在，在文件配置中第一段表示不同文件。
    /// </remarks>
    public interface IConfigProvider
    {
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
        /// <summary>配置项集合</summary>
        public IDictionary<String, String> Items { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>获取 或 设置 配置值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public virtual String this[String key] { get => Items[key]; set => Items[key] = value; }
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
        protected virtual void MapTo(IDictionary<String, String> source, Object model, String nameSpace)
        {
            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;

                var name = pi.Name;
                if (!nameSpace.IsNullOrEmpty()) name = $"{nameSpace}:{pi.Name}";

                // 分别处理基本类型和复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    if (source.TryGetValue(name, out var str)) pi.SetValue(model, str.ChangeType(pi.PropertyType), null);
                }
                else
                {
                    // 复杂类型需要递归处理
                    var val = pi.GetValue(model, null);
                    if (val == null)
                    {
                        // 如果有无参构造函数，则实例化一个
                        var ci = pi.PropertyType.GetConstructor(new Type[0]);
                        if (ci != null) val = ci.Invoke(null);
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
        public virtual void Save<T>(T model, String nameSpace = null)
        {
            MapFrom(Items, model, nameSpace);
        }

        /// <summary>从公有实例属性映射到字典</summary>
        /// <param name="source"></param>
        /// <param name="model"></param>
        /// <param name="nameSpace"></param>
        protected virtual void MapFrom(IDictionary<String, String> source, Object model, String nameSpace)
        {
            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;

                var name = pi.Name;
                if (!nameSpace.IsNullOrEmpty()) name = $"{nameSpace}:{pi.Name}";

                var val = pi.GetValue(model, null);

                // 分别处理基本类型和复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    // 格式化为字符串，主要处理时间日期格式
                    source[name] = "{0}".F(val);
                }
                else
                {
                    // 递归映射
                    if (val != null) MapFrom(source, val, name);
                }
            }
        }

        /// <summary>合并源字典到目标字典</summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="nameSpace"></param>
        protected virtual void Merge(IDictionary<String, String> source, IDictionary<String, String> dest, String nameSpace)
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
        public virtual void Bind<T>(T model, String nameSpace = null)
        {
            MapTo(Items, model, nameSpace);
        }
        #endregion
    }

    /// <summary>文件配置提供者</summary>
    public class FileConfigProvider: ConfigProvider
    {
        /// <summary>文件名。最高优先级，优先于模型特性指定的文件名</summary>
        public String FileName { get; set; }

        /// <summary>获取模型的文件名</summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        protected virtual String GetFileName(Type modelType)
        {
            var fileName = FileName;

            // 从模型类头部特性获取文件名路径，Xml/Json 是为了兼容旧版本
            if (fileName.IsNullOrEmpty())
            {
                var atts = modelType.GetCustomAttributes(typeof(ConfigFileAttribute), false);
                if (atts != null && atts.Length > 0 && atts[0] is ConfigFileAttribute xcf) fileName = xcf.FileName;
            }
            if (fileName.IsNullOrEmpty())
            {
                var atts = modelType.GetCustomAttributes(typeof(XmlConfigFileAttribute), false);
                if (atts != null && atts.Length > 0 && atts[0] is XmlConfigFileAttribute xcf) fileName = xcf.FileName;
            }
            if (fileName.IsNullOrEmpty())
            {
                var atts = modelType.GetCustomAttributes(typeof(JsonConfigFileAttribute), false);
                if (atts != null && atts.Length > 0 && atts[0] is JsonConfigFileAttribute xcf) fileName = xcf.FileName;
            }

            //if (fileName.IsNullOrEmpty()) fileName = FileName;

            //todo 这里需要想办法修改配置根目录

            return fileName;
        }
    }
}