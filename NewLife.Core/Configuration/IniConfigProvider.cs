using System;
using System.IO;
using System.Text;
using NewLife.Log;

namespace NewLife.Configuration
{
    /// <summary>Ini文件配置提供者</summary>
    /// <remarks>
    /// 支持从不同配置文件加载到不同配置模型
    /// </remarks>
    public class InIConfigProvider : FileConfigProvider
    {
        /// <summary>初始化</summary>
        /// <param name="value"></param>
        public override void Init(String value)
        {
            // 加上默认后缀
            if (!value.IsNullOrEmpty() && Path.GetExtension(value).IsNullOrEmpty()) value += ".ini";

            base.Init(value);
        }

        /// <summary>读取配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnRead(String fileName, IConfigSection section)
        {
            var lines = File.ReadAllLines(fileName);

            var currentSection = section;
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
                    currentSection = section.GetOrAddChild(str.Trim('[', ']'));
                    currentSection.Comment = remark;
                }
                else
                {
                    var p = str.IndexOf('=');
                    if (p > 0)
                    {
                        var name = str.Substring(0, p).Trim();

                        // 构建配置值和注释
                        var cfg = currentSection.AddChild(name);
                        if (p + 1 < str.Length) cfg.Value = str.Substring(p + 1).Trim();
                        cfg.Comment = remark;
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
            var str = GetString(section);
            var old = "";
            if (File.Exists(fileName)) old = File.ReadAllText(fileName);

            if (str != old)
            {
                XTrace.WriteLine("保存配置 {0}", fileName);

                File.WriteAllText(fileName, str);
            }
        }

        /// <summary>获取字符串形式</summary>
        /// <param name="section">配置段</param>
        /// <returns></returns>
        public override String GetString(IConfigSection section = null)
        {
            if (section == null) section = Root;

            // 分组写入
            var sb = new StringBuilder();
            foreach (var item in section.Childs)
            {
                if (item.Childs != null && item.Childs.Count > 0)
                {
                    // 段前空一行
                    sb.AppendLine();
                    // 注释
                    if (!item.Comment.IsNullOrEmpty()) sb.AppendLine("; " + item.Comment);
                    sb.AppendLine($"[{item.Key}]");

                    // 写入当前段
                    foreach (var elm in item.Childs)
                    {
                        // 注释
                        if (!elm.Comment.IsNullOrEmpty()) sb.AppendLine("; " + elm.Comment);

                        sb.AppendLine($"{elm.Key} = {elm.Value}");
                    }
                }
                else
                {
                    // 注释
                    if (!item.Comment.IsNullOrEmpty()) sb.AppendLine("; " + item.Comment);

                    sb.AppendLine($"{item.Key} = {item.Value}");
                }
            }

            return sb.ToString();
        }
    }
}