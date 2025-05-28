using System.Text.RegularExpressions;
using NewLife.Collections;

namespace NewLife.Web;

/// <summary>超链接</summary>
public class Link
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; } = null!;

    /// <summary>全名</summary>
    public String? FullName { get; set; }

    /// <summary>超链接</summary>
    public String? Url { get; set; }

    /// <summary>原始超链接</summary>
    public String? RawUrl { get; set; }

    /// <summary>标题</summary>
    public String? Title { get; set; }

    /// <summary>版本</summary>
    public Version? Version { get; set; }

    /// <summary>时间</summary>
    public DateTime Time { get; set; }

    /// <summary>哈希</summary>
    public String? Hash { get; set; }

    /// <summary>原始Html</summary>
    public String? Html { get; set; }
    #endregion

    #region 方法
    static readonly Regex _regA = new("""<a[^>]* href=?"(?<链接>[^>"]*)?"[^>]*>(?<名称>[^<]*)</a>\s*</td>[^>]*<td[^>]*>(?<哈希>[^<]*)</td>""", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex _regB = new("""<td>(?<时间>[^<]*)</td>\s*<td>(?<大小>[^<]*)</td>\s*<td>\s*<a[^>]* href="?(?<链接>[^>"]*)"?[^>]*>(?<名称>[^<]*)</a>\s*</td>[^>]*<td[^>]*>(?<哈希>[^<]*)</td>""", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex _regTitle = new("""title=("?)(?<标题>[^ ']*?)\1""", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>分析HTML中的链接</summary>
    /// <param name="html">Html文本</param>
    /// <param name="baseUrl">基础Url，用于生成超链接的完整Url</param>
    /// <param name="filter">用于基础过滤的过滤器</param>
    /// <returns></returns>
    public static Link[] Parse(String html, String? baseUrl = null, Func<Link, Boolean>? filter = null)
    {
        // baseurl必须是/结尾
        if (baseUrl != null && !baseUrl.EndsWith("/")) baseUrl += "/";
        if (baseUrl.StartsWithIgnoreCase("ftp://")) return ParseFTP(html, baseUrl, filter);

        // 分析所有链接
        var list = new List<Link>();
        var buri = baseUrl.IsNullOrEmpty() ? null : new Uri(baseUrl);
        var ms = _regB.Matches(html);
        if (ms.Count == 0) ms = _regA.Matches(html);
        foreach (var item in ms)
        {
            if (item is not Match match) continue;

            var link = new Link
            {
                Html = match.Value,
                FullName = match.Groups["名称"].Value.Trim(),
                Url = match.Groups["链接"].Value.Trim(),
                Hash = match.Groups["哈希"].Value.Trim(),
                Time = match.Groups["时间"].Value.Trim().ToDateTime(),
            };
            if (link.Hash.Contains("&lt;")) link.Hash = null;
            link.RawUrl = link.Url;
            link.Name = link.FullName;

            // 过滤器
            if (filter != null && !filter(link)) continue;

            link.Url = link.Url.TrimStart("#");
            if (String.IsNullOrEmpty(link.Url)) continue;

            if (link.Url.StartsWithIgnoreCase("javascript:")) continue;

            //// 分析title
            //var txt = match.Groups["其它1"].Value.Trim();
            //if (txt.IsNullOrWhiteSpace() || !_regTitle.IsMatch(txt)) txt = match.Groups["其它2"].Value.Trim();
            //var mc = _regTitle.Match(txt);
            //if (mc.Success)
            //{
            //    link.Title = mc.Groups["标题"].Value.Trim();
            //}

            // 完善下载地址
            if (buri != null)
            {
                var uri = new Uri(buri, link.RawUrl);
                link.Url = uri.ToString();
            }
            else
            {
                link.Url = link.RawUrl;
            }

            // 从github.com下载需要处理Url
            if (link.Url.Contains("github.com") && link.Url.Contains("/blob/")) link.Url = link.Url.Replace("/blob/", "/raw/");

            // 分割名称，计算结尾的时间 yyyyMMddHHmmss
            //if (link.Time.Year < 1000)
            link.ParseTime();

            // 分割版本，_v1.0.0.0
            link.ParseVersion();

            // 去掉后缀，特殊处理.tar.gz双后缀
            var name = link.Name;
            if (name.EndsWithIgnoreCase(".tar.gz"))
                link.Name = name[..^7];
            else
            {
                var p = name.LastIndexOf('.');
                if (p > 0) link.Name = name[..p];
            }

            list.Add(link);
        }

        return list.ToArray();
    }

    private static Link[] ParseFTP(String html, String? baseUrl, Func<Link, Boolean>? filter = null)
    {
        var list = new List<Link>();

        var ns = html.Split("\r\n", "\r", "\n");
        if (ns.Length == 0) return list.ToArray();

        // 如果由很多段组成，可能是unix格式
        _ = ns[0].Split(' ').Length >= 6;
        var buri = baseUrl.IsNullOrEmpty() ? null : new Uri(baseUrl);
        foreach (var item in ns)
        {
            var link = new Link
            {
                FullName = item
            };
            link.Name = link.FullName;
            //link.Name = Path.GetFileNameWithoutExtension(item);
            //link.Url = new Uri(buri, item).ToString();
            //link.RawUrl = link.Url;

            // 过滤器
            if (filter != null && !filter(link)) continue;

            // 分析title
            link.Title = Path.GetFileNameWithoutExtension(item);

            // 完善下载地址
            if (buri != null)
            {
                var uri = new Uri(buri, item);
                link.Url = uri.ToString();
            }
            else
            {
                link.Url = item;
            }

            //if (link.Time.Year < 1000)
            {
                // 分割名称，计算结尾的时间 yyyyMMddHHmmss
                var idx = link.ParseTime();
                if (idx > 0) link.Title = link.Title[..idx];
            }

            {
                // 分割版本，_v1.0.0.0
                var idx = link.ParseVersion();
                if (idx > 0) link.Title = link.Title[..idx];
            }

            // 去掉后缀，特殊处理.tar.gz双后缀
            var name = link.Name;
            if (name.EndsWithIgnoreCase(".tar.gz"))
                link.Name = name[..^7];
            else
            {
                var p = name.LastIndexOf('.');
                if (p > 0) link.Name = name[..p];
            }

            list.Add(link);
        }

        return list.ToArray();
    }

    /// <summary>分解文件</summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public Link Parse(String file)
    {
        RawUrl = file;
        Url = file.GetFullPath();
        FullName = Path.GetFileName(file);
        Name = FullName;

        ParseTime();
        ParseVersion();

        // 去掉后缀，特殊处理.tar.gz双后缀
        var name = Name;
        if (name.EndsWithIgnoreCase(".tar.gz"))
            Name = name[..^7];
        else
        {
            var p = name.LastIndexOf('.');
            if (p > 0) Name = name[..p];
        }

        // 时间
        if (Time.Year < 2000)
        {
            var fi = file.AsFile();
            if (fi != null && fi.Exists) Time = fi.LastWriteTime;
        }

        return this;
    }

    /// <summary>从名称分解时间</summary>
    /// <returns></returns>
    public Int32 ParseTime()
    {
        var name = Name;
        if (name.IsNullOrEmpty()) return -1;

        // 分割名称，计算结尾的时间 yyyyMMddHHmmss
        var p = name.LastIndexOf("_");
        if (p <= 0) return -1;

        var ts = name[(p + 1)..];
        if (ts.StartsWith("20") && ts.Length >= 4 + 2 + 2 + 2 + 2 + 2)
        {
            Time = new DateTime(
                ts[..4].ToInt(),
                ts.Substring(4, 2).ToInt(),
                ts.Substring(6, 2).ToInt(),
                ts.Substring(8, 2).ToInt(),
                ts.Substring(10, 2).ToInt(),
                ts.Substring(12, 2).ToInt());

            Name = name[..p] + name[(p + 1 + 14)..];
        }
        else if (ts.StartsWith("20") && ts.Length >= 4 + 2 + 2)
        {
            Time = new DateTime(
                ts[..4].ToInt(),
                ts.Substring(4, 2).ToInt(),
                ts.Substring(6, 2).ToInt());

            Name = name[..p] + name[(p + 1 + 8)..];
        }

        return p;
    }

    /// <summary>从名称分解版本</summary>
    /// <returns></returns>
    public Int32 ParseVersion()
    {
        var name = Name;
        if (name.IsNullOrEmpty()) return -1;

        // 分割版本，_v1.0.0.0
        var p = IndexOfAny(name, ["_v", "_V", ".v", ".V", " v", " V"], 0);
        if (p <= 0) return -1;

        // 后续位置
        var p2 = name.IndexOfAny([' ', '_', '-'], p + 2);
        if (p2 < 0)
        {
            p2 = name.LastIndexOf('.');
            if (p2 <= p) p2 = -1;
        }
        if (p2 < 0) p2 = name.Length;

        // 尾部截断
        var vs = name.Substring(p + 2, p2 - p - 2);
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

            var str = name[..p];
            if (p2 < name.Length) str += name[p2..];
            Name = str;
        }

        // 返回位置
        return p;
    }

    private static Int32 IndexOfAny(String str, String[] anyOf, Int32 startIndex)
    {
        foreach (var item in anyOf)
        {
            var p = str.IndexOf(item, startIndex);
            if (p >= 0) return p;
        }

        return -1;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("{0} {1}", Name, RawUrl);
        if (Version != null) sb.AppendFormat(" v{0}", Version);
        if (Time > DateTime.MinValue) sb.AppendFormat(" {0}", Time.ToFullString());

        return sb.Return(true);
    }
    #endregion
}