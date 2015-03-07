using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NewLife.Reflection;

namespace NewLife.Web
{
    /// <summary>超链接</summary>
    public class Link
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _Url;
        /// <summary>超链接</summary>
        public String Url { get { return _Url; } set { _Url = value; } }

        private String _RawUrl;
        /// <summary>原始超链接</summary>
        public String RawUrl { get { return _RawUrl; } set { _RawUrl = value; } }

        private String _Title;
        /// <summary>标题</summary>
        public String Title { get { return _Title; } set { _Title = value; } }

        private Version _Version;
        /// <summary>版本</summary>
        public Version Version { get { return _Version; } set { _Version = value; } }

        private DateTime _Time;
        /// <summary>时间</summary>
        public DateTime Time { get { return _Time; } set { _Time = value; } }

        private String _Html;
        /// <summary>原始Html</summary>
        public String Html { get { return _Html; } set { _Html = value; } }
        #endregion

        #region 方法
        static Regex _regA = new Regex("<a(?<其它1>[^>]*) href=?\"(?<链接>[^>\"]*)?\"(?<其它2>[^>]*)>(?<名称>[^<]*)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        static Regex _regTitle = new Regex("title=(\"?)(?<标题>[^ \']*?)\\1", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>分析HTML中的链接</summary>
        /// <param name="html">Html文本</param>
        /// <param name="baseurl">基础Url，用于生成超链接的完整Url</param>
        /// <param name="filter">用于基础过滤的过滤器</param>
        /// <returns></returns>
        public static Link[] Parse(String html, String baseurl = null, Func<Link, Boolean> filter = null)
        {
            // 分析所有链接
            var list = new List<Link>();
            var buri = new Uri(baseurl);
            foreach (Match item in _regA.Matches(html))
            {
                var link = new Link();

                link.Html = item.Value;
                link.Name = item.Groups["名称"].Value.Trim();
                link.Url = item.Groups["链接"].Value.Trim();
                link.RawUrl = link.Url;

                // 过滤器
                if (filter != null && !filter(link)) continue;

                link.Url = link.Url.TrimStart("#");
                if (String.IsNullOrEmpty(link.Url)) continue;

                if (link.Url.StartsWithIgnoreCase("javascript:")) continue;

                // 分析title
                var txt = item.Groups["其它1"].Value.Trim();
                if (txt.IsNullOrWhiteSpace() || !_regTitle.IsMatch(txt)) txt = item.Groups["其它2"].Value.Trim();
                var mc = _regTitle.Match(txt);
                if (mc.Success)
                {
                    link.Title = mc.Groups["标题"].Value.Trim();
                }

                // 完善下载地址
                var uri = new Uri(buri, link.RawUrl);
                link.Url = uri.ToString();

                // 分割名称，计算结尾的时间 yyyyMMddHHmmss
                link.ParseTime();

                // 分割版本，_v1.0.0.0
                link.ParseVersion();

                list.Add(link);
            }

            return list.ToArray();
        }

        void ParseTime()
        {
            // 分割名称，计算结尾的时间 yyyyMMddHHmmss
            var p = Name.LastIndexOf("_");
            if (p > 0)
            {
                var ts = Name.Substring(p + 1);
                if (ts.StartsWith("20") && ts.Length >= 4 + 2 + 2 + 2 + 2 + 2)
                {
                    Time = new DateTime(
                        ts.Substring(0, 4).ToInt(),
                        ts.Substring(4, 2).ToInt(),
                        ts.Substring(6, 2).ToInt(),
                        ts.Substring(8, 2).ToInt(),
                        ts.Substring(10, 2).ToInt(),
                        ts.Substring(12, 2).ToInt());
                }
            }
        }

        void ParseVersion()
        {
            // 分割版本，_v1.0.0.0
            var vs = Name.CutStart("_v", "_V");
            if (vs == Name)
            {
                // 也可能没有v，但是这是必须有圆点
                if (Name.Contains(".") && (Name.Contains(" ") || Name.Contains("_")))
                {
                    vs = Name.CutStart(" ", "_");
                    if (!Name.Contains(".")) return;
                }
            }
            if (vs != Name)
            {
                // 尾部截断
                vs = vs.CutEnd(" ", "_", "-");
                // 有可能只有_v1，而没有子版本
                var ss = vs.SplitAsInt(".");
                if (ss.Length > 0)
                {
                    switch (ss.Length)
                    {
                        case 1:
                            Version = new Version(ss[0], 0);
                            break;
                        case 2:
                            Version = new Version(ss[0], ss[1]);
                            break;
                        case 3:
                            Version = new Version(ss[0], ss[1], ss[2]);
                            break;
                        case 4:
                            Version = new Version(ss[0], ss[1], ss[2], ss[3]);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //return base.ToString();
            //return "{0} {1} {2} {3}".F(Name, RawUrl, Version, Time);
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}", Name, RawUrl);
            if (Version != null) sb.AppendFormat(" {0}", Version);
            if (Time > DateTime.MinValue) sb.AppendFormat(" {0}", Time.ToFullString());

            return sb.ToString();
        }
        #endregion
    }
}