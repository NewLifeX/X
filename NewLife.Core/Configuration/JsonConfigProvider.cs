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
        /// <summary>初始化</summary>
        /// <param name="value"></param>
        public override void Init(String value)
        {
            // 加上默认后缀
            if (!value.IsNullOrEmpty() && Path.GetExtension(value).IsNullOrEmpty()) value += ".json";

            base.Init(value);
        }

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
            var json = GetString(section);
            File.WriteAllText(fileName, json);
        }

        /// <summary>获取字符串形式</summary>
        /// <param name="section">配置段</param>
        /// <returns></returns>
        public override String GetString(IConfigSection section = null)
        {
            if (section == null) section = Root;

            var rs = new Dictionary<String, Object>();
            Map(section, rs);

            var jw = new JsonWriter
            {
                IgnoreNullValues = false,
                IgnoreComment = false,
                Indented = true,
            };

            jw.Write(rs);

            // 输出，并格式化
            var json = jw.GetString();
            //json = JsonHelper.Format(json);

            return json;
        }
    }
}