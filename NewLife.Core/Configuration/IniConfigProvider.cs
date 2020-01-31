using System;
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
        /// <summary>读取配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnRead(String fileName, IConfigSection section)
        {
            var lines = File.ReadAllLines(fileName);

            var sec = "";
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
                    sec = str.Trim('[', ']');
                }
                else
                {
                    var p = str.IndexOf('=');
                    if (p > 0)
                    {
                        var name = str.Substring(0, p).Trim();
                        if (!sec.IsNullOrEmpty()) name = $"{sec}:{name}";

                        // 构建配置值和注释
                        var cfg = new ConfigSection
                        {
                            Key = name,
                            Value = str.Substring(p + 1).Trim(),
                            Description = remark
                        };
                        section.Childs.Add(cfg);
                    }
                }

                // 清空注释
                remark = null;
            }
        }

        /// <summary>写入配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnWrite(String fileName, IConfigSection section)
        {
            // 分组写入
            var sb = new StringBuilder();
            foreach (var item in section.Childs)
            {
                if (item.Childs != null && item.Childs.Count > 0)
                {
                    // 段前空一行
                    sb.AppendLine();
                    sb.AppendLine($"[{item.Key}]");

                    // 写入当前段
                    foreach (var elm in item.Childs)
                    {
                        // 注释
                        if (!elm.Description.IsNullOrEmpty()) sb.AppendLine("; " + elm.Description);

                        sb.AppendLine($"{elm.Key} = {elm.Value}");
                    }
                }
                else
                {
                    // 注释
                    if (!item.Description.IsNullOrEmpty()) sb.AppendLine("; " + item.Description);

                    sb.AppendLine($"{item.Key} = {item.Value}");
                }
            }

            File.WriteAllText(fileName, sb.ToString());
        }
    }
}