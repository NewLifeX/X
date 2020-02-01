using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Json;
using NewLife.Xml;

namespace NewLife.Configuration
{
    /// <summary>文件配置提供者</summary>
    /// <remarks>
    /// 每个提供者实例对应一个配置文件，支持热更新
    /// </remarks>
    public abstract class FileConfigProvider : ConfigProvider
    {
        /// <summary>文件名。最高优先级，优先于模型特性指定的文件名</summary>
        public String FileName { get; set; }

        /// <summary>模型类。兼容旧版配置类，用于识别配置头</summary>
        public Type ModelType { get; set; }

        /// <summary>获取模型的文件名</summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        protected virtual String GetFileName(Type modelType)
        {
            var fileName = FileName;

            // 从模型类头部特性获取文件名路径，Xml/Json 是为了兼容旧版本
            if (modelType != null)
            {
                if (fileName.IsNullOrEmpty())
                {
                    var atts = modelType.GetCustomAttributes(typeof(ConfigFileAttribute), false);
                    if (atts != null && atts.Length > 0 && atts[0] is ConfigFileAttribute cf) fileName = cf.FileName;
                }
                if (fileName.IsNullOrEmpty())
                {
                    var atts = modelType.GetCustomAttributes(typeof(XmlConfigFileAttribute), false);
                    if (atts != null && atts.Length > 0 && atts[0] is XmlConfigFileAttribute cf) fileName = cf.FileName;
                }
                if (fileName.IsNullOrEmpty())
                {
                    var atts = modelType.GetCustomAttributes(typeof(JsonConfigFileAttribute), false);
                    if (atts != null && atts.Length > 0 && atts[0] is JsonConfigFileAttribute cf) fileName = cf.FileName;
                }
            }

            return fileName;
        }

        /// <summary>加载配置</summary>
        public override void LoadAll()
        {
            // 准备文件名
            var fileName = GetFileName(ModelType);
            if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(FileName));

            fileName = fileName.GetBasePath();

            if (!File.Exists(fileName)) throw new FileNotFoundException("找不到文件", fileName);

            // 读取文件
            OnRead(fileName, Root);
        }

        /// <summary>加载配置到模型</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        /// <returns></returns>
        public override T Load<T>(String nameSpace = null)
        {
            ModelType = typeof(T);

            return base.Load<T>(nameSpace);
        }

        /// <summary>读取配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected abstract void OnRead(String fileName, IConfigSection section);

        /// <summary>保存配置树到数据源</summary>
        public override void SaveAll()
        {
            // 准备文件名
            var fileName = GetFileName(ModelType);
            if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(FileName));

            fileName = fileName.GetBasePath();
            fileName.EnsureDirectory(true);

            // 写入文件
            OnWrite(fileName, Root);
        }

        /// <summary>写入配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected abstract void OnWrite(String fileName, IConfigSection section);

        #region 辅助
        /// <summary>多层字典映射为一层</summary>
        /// <param name="src"></param>
        /// <param name="section"></param>
        protected virtual void Map(IDictionary<String, Object> src, IConfigSection section)
        {
            foreach (var item in src)
            {
                var name = item.Key;
                if (name[0] == '#') continue;

                var cfg = section.GetOrAddChild(name);
                var cname = "#" + name;
                if (src.TryGetValue(cname, out var comment) && comment != null) cfg.Comment = comment + "";

                // 仅支持内层字典，不支持内层数组
                if (item.Value is IDictionary<String, Object> dic)
                    Map(dic, cfg);
                else
                    cfg.Value = "{0}".F(item.Value);
            }
        }

        /// <summary>一层字典映射为多层</summary>
        /// <param name="section"></param>
        /// <param name="dst"></param>
        protected virtual void Map(IConfigSection section, IDictionary<String, Object> dst)
        {
            foreach (var item in section.Childs)
            {
                // 注释
                if (!item.Comment.IsNullOrEmpty()) dst["#" + item.Key] = item.Comment;

                if (item.Childs == null)
                {
                    dst[item.Key] = item.Value;
                }
                else
                {
                    var rs = new Dictionary<String, Object>();
                    Map(item, rs);

                    dst[item.Key] = rs;
                }
            }
        }
        #endregion
    }
}