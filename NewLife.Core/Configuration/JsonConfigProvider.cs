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
        /// <summary>读取配置文件，得到字典</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected override IDictionary<String, String> OnRead(String fileName)
        {
            var txt = File.ReadAllText(fileName);
            var json = new JsonParser(txt);
            var src = json.Decode() as IDictionary<String, Object>;

            var rs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            Map(src, rs, null);

            return rs;
        }

        /// <summary>把字典写入配置文件</summary>
        /// <param name="fileName"></param>
        /// <param name="source"></param>
        protected override void OnWrite(String fileName, IDictionary<String, String> source)
        {
            var json = source.ToJson(true, true, false);

            File.WriteAllText(fileName, json);
        }
    }
}