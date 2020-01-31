using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Json;
using NewLife.Xml;

namespace NewLife.Configuration
{
    /// <summary>文件配置提供者</summary>
    public abstract class FileConfigProvider : ConfigProvider
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

        /// <summary>获取模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。映射时去掉</param>
        /// <returns>模型实例</returns>
        public override T Load<T>(String nameSpace = null)
        {
            var fileName = GetFileName(typeof(T));

            // 这里需要想办法修改根目录
            fileName = fileName.GetBasePath();

            var source = OnRead(fileName);
            Merge(source, Items, nameSpace);

            var model = new T();
            MapTo(source, model, null);

            return model;
        }

        /// <summary>读取配置文件，得到字典</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected abstract IDictionary<String, String> OnRead(String fileName);

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。映射时加上</param>
        public override void Save<T>(T model, String nameSpace = null)
        {
            //base.Save<T>(model, nameSpace);

            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            MapFrom(dic, model, nameSpace);
            Merge(dic, Items, null);

            var fileName = GetFileName(typeof(T));
            fileName = fileName.GetBasePath();
            fileName.EnsureDirectory(true);

            OnWrite(fileName, dic);
        }

        /// <summary>把字典写入配置文件</summary>
        /// <param name="fileName"></param>
        /// <param name="source"></param>
        protected abstract void OnWrite(String fileName, IDictionary<String, String> source);

        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。映射时去掉</param>
        public override void Bind<T>(T model, String nameSpace = null)
        {
            var fileName = GetFileName(typeof(T));

            // 这里需要想办法修改根目录
            fileName = fileName.GetBasePath();

            var source = OnRead(fileName);
            Merge(source, Items, nameSpace);

            MapTo(source, model, null);
        }

        #region 辅助
        /// <summary>多层字典映射为一层</summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="nameSpace"></param>
        protected virtual void Map(IDictionary<String, Object> src, IDictionary<String, String> dst, String nameSpace)
        {
            foreach (var item in src)
            {
                var name = item.Key;
                if (!nameSpace.IsNullOrEmpty()) name = $"{nameSpace}:{item.Key}";

                // 仅支持内层字典，不支持内层数组
                if (item.Value is IDictionary<String, Object> dic)
                    Map(dic, dst, name);
                else
                    dst[name] = "{0}".F(item.Value);
            }
        }

        /// <summary>一层字典映射为多层</summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        protected virtual void Map(IDictionary<String, String> src, IDictionary<String, Object> dst)
        {
            // 按照配置段分组，相同组写在一起
            var dic = new Dictionary<String, Dictionary<String, String>>();

            foreach (var item in src)
            {
                var section = "";

                var name = item.Key;
                var p = item.Key.IndexOf(':');
                if (p > 0)
                {
                    section = item.Key.Substring(0, p);
                    name = item.Key.Substring(p + 1);
                }

                if (!dic.TryGetValue(section, out var dic2)) dic[section] = dic2 = new Dictionary<String, String>();

                dic2[name] = item.Value;
            }

            // 转多层
            foreach (var item in dic)
            {
                if (item.Key.IsNullOrEmpty())
                {
                    foreach (var elm in item.Value)
                    {
                        dst[elm.Key] = elm.Value;
                    }
                }
                else
                {
                    var rs = new Dictionary<String, Object>();
                    Map(item.Value, rs);

                    dst[item.Key] = rs;
                }
            }
        }
        #endregion
    }
}