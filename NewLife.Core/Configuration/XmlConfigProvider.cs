using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace NewLife.Configuration
{
    /// <summary>Xml文件配置提供者</summary>
    /// <remarks>
    /// 支持从不同配置文件加载到不同配置模型
    /// </remarks>
    public class XmlConfigProvider : FileConfigProvider
    {
        /// <summary>获取模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="nameSpace">命名空间。映射时去掉</param>
        /// <returns>模型实例</returns>
        public override T Load<T>(String nameSpace = null)
        {
            var fileName = GetFileName(typeof(T));

            // 这里需要想办法修改根目录
            fileName = fileName.GetFullPath();

            var source = Read(fileName);
            Merge(source, Items, nameSpace);

            var model = new T();
            MapTo(source, model, null);

            return model;
        }

        protected virtual IDictionary<String, String> Read(String fileName)
        {
            using var fs = File.OpenRead(fileName);
            using var reader = XmlReader.Create(fs);

            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            reader.ReadStartElement();

            return dic;
        }
    }
}