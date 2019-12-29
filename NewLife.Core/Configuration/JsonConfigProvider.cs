using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Configuration
{
    /// <summary>Json文件配置提供者</summary>
    /// <remarks>
    /// 支持从不同配置文件加载到不同配置模型
    /// </remarks>
    public class JsonConfigProvider : FileConfigProvider
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

        /// <summary>读取配置文件，得到字典</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected virtual IDictionary<String, String> Read(String fileName)
        {
            var txt = File.ReadAllText(fileName);
            var json = new JsonParser(txt);
            var src = json.Decode() as IDictionary<String, Object>;

            var rs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            Map(src, rs, null);

            return rs;
        }

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

            var json = dic.ToJson(true, true, false);

            var fileName = GetFileName(typeof(T));
            fileName = fileName.GetFullPath();

            File.WriteAllText(fileName, json);
        }
    }
}