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
        protected override IDictionary<String, ConfigItem> OnRead(String fileName)
        {
            var lines = File.ReadAllLines(fileName);
            var dic = new Dictionary<String, ConfigItem>(StringComparer.OrdinalIgnoreCase);

            var section = "";
            var remark = "";
            foreach (var item in lines)
            {
                var str = item.Trim();
                if (str.IsNullOrEmpty()) continue;

                // 读取注释
                if (str[0] == '#' || str[0] == ';')
                {
                    remark = str.TrimStart('#', ';').Trim();
                    continue;
                }

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

                        // 构建配置值和注释
                        dic[name] = new ConfigItem
                        {
                            Key = name,
                            Value = str.Substring(p + 1).Trim(),
                            Description = remark
                        };
                    }
                }

                // 清空注释
                remark = null;
            }

            return dic;
        }

        /// <summary>把字典写入配置文件</summary>
        /// <param name="fileName"></param>
        /// <param name="source"></param>
        protected override void OnWrite(String fileName, IDictionary<String, ConfigItem> source)
        {
            var dic = new Dictionary<String, Object>();
            Map(source, dic);

            // 分组写入
            //todo 需要写入Ini注释
            var sb = new StringBuilder();
            foreach (var item in dic)
            {
                if (item.Value is IDictionary<String, Object> dic2)
                {
                    // 段前空一行
                    sb.AppendLine();
                    sb.AppendLine($"[{item.Key}]");

                    // 写入当前段
                    foreach (var elm in dic2)
                    {
                        if (elm.Value is ConfigItem ci)
                        {
                            // 注释
                            if (!ci.Description.IsNullOrEmpty()) sb.AppendLine("; " + ci.Description);

                            sb.AppendLine($"{elm.Key} = {ci.Value}");
                        }
                    }
                }
                else if (item.Value is ConfigItem ci)
                {
                    // 注释
                    if (!ci.Description.IsNullOrEmpty()) sb.AppendLine("; " + ci.Description);

                    sb.AppendLine($"{item.Key} = {ci.Value}");
                }
            }

            File.WriteAllText(fileName, sb.ToString());
        }
    }
}