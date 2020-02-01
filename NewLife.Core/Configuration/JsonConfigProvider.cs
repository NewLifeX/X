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
        /// <summary>读取配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnRead(String fileName, IConfigSection section)
        {
            var txt = File.ReadAllText(fileName);
            var json = new JsonParser(txt);
            var src = json.Decode() as IDictionary<String, Object>;

            Map(src, section);
        }

        /// <summary>写入配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnWrite(String fileName, IConfigSection section)
        {
            var rs = new Dictionary<String, Object>();
            Map(section, rs);

            var jw = new JsonWriter
            {
                IgnoreNullValues = false,
                IgnoreComment = false,
            };

            jw.Write(rs);

            // 输出，并格式化
            var json = jw.GetString();
            json = JsonHelper.Format(json);

            File.WriteAllText(fileName, json);
        }
    }
}