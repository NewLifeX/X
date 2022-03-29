using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NewLife;
using XCode.DataAccessLayer;

namespace XCode.Configuration
{
    /// <summary>Sql模版，包含一个Sql语句在不同数据库下的多种写法</summary>
    public class SqlTemplate
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>默认Sql语句</summary>
        public String Sql { get; set; }

        /// <summary>特定数据库语句</summary>
        public IDictionary<String, String> Sqls { get; } = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region 方法
        /// <summary>分析字符串</summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Boolean Parse(String text) => Parse(new StringReader(text));

        /// <summary>分析数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public Boolean Parse(Stream stream) => Parse(new StreamReader(stream));

        /// <summary>从文本读写器中解析</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public Boolean Parse(TextReader reader)
        {
            var sb = new StringBuilder();
            var name = "";

            while (true)
            {
                var line = reader.ReadLine();
                if (line == null) break;

                if (!line.IsNullOrEmpty())
                {
                    // 识别数据库类型
                    if (line.StartsWith("--"))
                    {
                        var p1 = line.IndexOf('[');
                        var p2 = line.IndexOf(']');
                        if (p1 > 0 && p2 > p1)
                        {
                            // 完成一个sql片段，开始新的片段
                            if (sb.Length > 0)
                            {
                                if (name.IsNullOrEmpty())
                                    Sql = sb.ToString().Trim();
                                else
                                    Sqls[name] = sb.ToString().Trim();
                                sb.Clear();
                            }

                            name = line.Substring(p1 + 1, p2 - p1 - 1);
                            line = null;
                        }
                    }

                    if (line != null) sb.AppendLine(line);
                }
            }

            // 完成一个sql片段
            if (sb.Length > 0)
            {
                if (name.IsNullOrEmpty())
                    Sql = sb.ToString().Trim();
                else
                    Sqls[name] = sb.ToString().Trim();
            }

            return true;
        }

        /// <summary>分析嵌入资源</summary>
        /// <param name="assembly"></param>
        /// <param name="nameSpace"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Boolean ParseEmbedded(Assembly assembly, String nameSpace, String name)
        {
            var name2 = !nameSpace.IsNullOrEmpty() ? (nameSpace + "." + name) : name;
            var ns = assembly.GetManifestResourceNames();
            var res = ns.FirstOrDefault(e => e.EqualIgnoreCase(name2));
            if (res.IsNullOrEmpty()) return false;

            Name = Path.GetFileNameWithoutExtension(name);

            return Parse(assembly.GetManifestResourceStream(res));
        }

        /// <summary>获取指定数据库的Sql，如果未指定，则返回默认</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public String GetSql(DatabaseType type)
        {
            if (Sqls.TryGetValue(type + "", out var sql)) return sql;

            return Sql;
        }
        #endregion
    }
}