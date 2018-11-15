using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NewLife.Collections;

namespace NewLife.Web
{
    /// <summary>超链接</summary>
    public class Link
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>超链接</summary>
        public String Url { get; set; }

        /// <summary>原始超链接</summary>
        public String RawUrl { get; set; }

        /// <summary>标题</summary>
        public String Title { get; set; }

        /// <summary>版本</summary>
        public Version Version { get; set; }

        /// <summary>时间</summary>
        public DateTime Time { get; set; }

        /// <summary>原始Html</summary>
        public String Html { get; set; }
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
            if (baseurl.StartsWithIgnoreCase("ftp://")) return ParseFTP(html, baseurl, filter);

            // 分析所有链接
            var list = new List<Link>();
            var buri = new Uri(baseurl);
            foreach (Match item in _regA.Matches(html))
            {
                var link = new Link
                {
                    Html = item.Value,
                    Name = item.Groups["名称"].Value.Trim(),
                    Url = item.Groups["链接"].Value.Trim()
                };
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

                // 从github.com下载需要处理Url
                if (link.Url.Contains("github.com") && link.Url.Contains("/blob/")) link.Url = link.Url.Replace("/blob/", "/raw/");

                // 分割名称，计算结尾的时间 yyyyMMddHHmmss
                link.ParseTime();

                // 分割版本，_v1.0.0.0
                link.ParseVersion();

                list.Add(link);
            }

            return list.ToArray();
        }

        private static Link[] ParseFTP(String html, String url, Func<Link, Boolean> filter = null)
        {
            var list = new List<Link>();

            var ns = html.Split(Environment.NewLine);
            if (ns.Length == 0) return list.ToArray();

            // 如果由很多段组成，可能是unix格式
            var unix = ns[0].Split(" ").Length >= 6;
            var buri = new Uri(url);
            foreach (var item in ns)
            {
                var link = new Link
                {
                    Name = item
                };
                //link.Name = Path.GetFileNameWithoutExtension(item);
                //link.Url = new Uri(buri, item).ToString();
                //link.RawUrl = link.Url;

                // 过滤器
                if (filter != null && !filter(link)) continue;

                // 分析title
                link.Title = Path.GetFileNameWithoutExtension(item);

                // 完善下载地址
                var uri = new Uri(buri, item);
                link.Url = uri.ToString();

                // 分割名称，计算结尾的时间 yyyyMMddHHmmss
                var idx = link.ParseTime();
                if (idx > 0) link.Title = link.Title.Substring(0, idx);

                // 分割版本，_v1.0.0.0
                idx = link.ParseVersion();
                if (idx > 0) link.Title = link.Title.Substring(0, idx);

                list.Add(link);
            }

            return list.ToArray();
        }

        Int32 ParseTime()
        {
            // 分割名称，计算结尾的时间 yyyyMMddHHmmss
            var p = Name.LastIndexOf("_");
            if (p <= 0) return -1;

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

            return p;
        }

        Int32 ParseVersion()
        {
            // 分割版本，_v1.0.0.0
            var vs = Name.CutStart("_v", "_V", ".v", ".V", "v", " V");
            if (vs == Name)
            {
                // 也可能没有v，但是这是必须有圆点
                if (Name.Contains(".") && (Name.Contains(" ") || Name.Contains("_")))
                {
                    vs = Name.CutStart(" ", "_");
                    if (!Name.Contains(".")) return -1;
                }
            }
            if (vs == Name) return -1;

            // 返回位置
            var p = Name.LastIndexOf(vs) - 2;

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

            // 返回位置
            return p;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("{0} {1}", Name, RawUrl);
            if (Version != null) sb.AppendFormat(" v{0}", Version);
            if (Time > DateTime.MinValue) sb.AppendFormat(" {0}", Time.ToFullString());

            return sb.Put(true);
        }
        #endregion
    }
}