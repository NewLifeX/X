using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewLife.Configuration
{
    /// <summary>Ini文件配置提供者</summary>
    /// <remarks>
    /// 支持从不同配置文件加载到不同配置模型
    /// </remarks>
    public class InIConfigProvider : FileConfigProvider
    {
        /// <summary>读取配置文件，得到字典</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected override IDictionary<String, String> OnRead(String fileName)
        {
            var lines = File.ReadAllLines(fileName);
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            var section = "";
            foreach (var item in lines)
            {
                var str = item.Trim();
                if (str.IsNullOrEmpty()) continue;
                if (str[0] == '#') continue;
                if (str[0] == ';') continue;

                if (str[0] == '[' && str[str.Length - 1] == ']')
                {
                    section = str.Trim('[', ']');
                }
                else
                {
                    var p = str.IndexOf('=');
                    if (p > 0)
                    {
                        var name = str.Substring(0, p).Trim();
                        if (!section.IsNullOrEmpty()) name = $"{section}:{name}";
                        dic[name] = str.Substring(p + 1).Trim();
                    }
                }
            }

            return dic;
        }

        /// <summary>把字典写入配置文件</summary>
        /// <param name="fileName"></param>
        /// <param name="source"></param>
        protected override void OnWrite(String fileName, IDictionary<String, String> source)
        {
            // 按照配置段分组，相同组写在一起
            var dic = new Dictionary<String, Dictionary<String, String>>();

            foreach (var item in source)
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

            // 分组写入
            //todo 需要写入Ini注释
            var sb = new StringBuilder();
            foreach (var item in dic)
            {
                if (!item.Key.IsNullOrEmpty())
                {
                    // 段前空一行
                    if (sb.Length > 0) sb.AppendLine();

                    sb.AppendLine($"[{item.Key}]");
                }

                // 写入当前段
                foreach (var elm in item.Value)
                {
                    sb.AppendLine($"{elm.Key} = {elm.Value}");
                }
            }

            File.WriteAllText(fileName, sb.ToString());
        }
    }
}