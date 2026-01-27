using System.Web;

namespace NewLife.Web;

/// <summary>资源定位。无限制解析Url地址</summary>
public class UriInfo
{
    #region 属性
    /// <summary>协议</summary>
    public String? Scheme { get; set; }

    /// <summary>主机</summary>
    public String? Host { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; }

    /// <summary>路径</summary>
    public String? AbsolutePath { get; set; }

    /// <summary>查询</summary>
    public String? Query { get; set; }

    /// <summary>路径与查询</summary>
    public String? PathAndQuery
    {
        get
        {
            if (Query.IsNullOrEmpty()) return AbsolutePath;

            if (Query[0] == '?') return AbsolutePath + Query;

            return $"{AbsolutePath}?{Query}";
        }
    }

    /// <summary>主机与端口。省略默认端口</summary>
    public String? Authority
    {
        get
        {
            if (Host.IsNullOrEmpty()) return Host;

            // 检测是否是IPv6地址（包含冒号且不是端口分隔符）
            var isIPv6 = Host.Contains(':');
            var hostPart = isIPv6 ? $"[{Host}]" : Host;

            if (Port == 0) return hostPart;

            // 检查是否为默认端口
            if (Scheme.EqualIgnoreCase("http", "ws"))
                return Port == 80 ? hostPart : $"{hostPart}:{Port}";
            else if (Scheme.EqualIgnoreCase("https", "wss"))
                return Port == 443 ? hostPart : $"{hostPart}:{Port}";

            return $"{hostPart}:{Port}";
        }
    }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public UriInfo() { }

    /// <summary>实例化</summary>
    /// <param name="value"></param>
    public UriInfo(String value) => Parse(value);
    #endregion

    #region 方法
    /// <summary>尝试解析Url字符串</summary>
    public static Boolean TryParse(String? value, out UriInfo? uriInfo)
    {
        uriInfo = null;
        if (value.IsNullOrWhiteSpace()) return false;

        uriInfo = new UriInfo();
        return uriInfo.Parse(value);
    }

    /// <summary>解析Url字符串</summary>
    /// <param name="value"></param>
    public Boolean Parse(String value)
    {
        if (value.IsNullOrWhiteSpace()) return false;

        var span = value.AsSpan();
        var p = 0;

        // 先处理头尾，再处理中间的主机和端口
        var schemeIndex = span.IndexOf("://".AsSpan());
        if (schemeIndex >= 0)
        {
            Scheme = span[..schemeIndex].ToString();
            p = schemeIndex + 3;
        }

        // 第二步找到/，它左边是主机和端口，右边是路径和查询。如果没有/，则整个字符串都是主机和端口
        var slashIndex = span[p..].IndexOf('/');
        if (slashIndex >= 0)
        {
            slashIndex += p;
            ParseHost(span[p..slashIndex]);
            ParsePath(span, slashIndex);
        }
        else
        {
            var queryIndex = span[p..].IndexOf('?');
            if (queryIndex >= 0)
            {
                queryIndex += p;
                ParseHost(span[p..queryIndex]);
                Query = span[queryIndex..].ToString();
            }
            else
            {
                ParseHost(span[p..]);
            }
        }

        // 如果主要部分都没有，标记为失败
        if (Scheme.IsNullOrEmpty() && Host.IsNullOrEmpty() && AbsolutePath.IsNullOrEmpty())
            return false;

        if (AbsolutePath.IsNullOrEmpty()) AbsolutePath = "/";

        return true;
    }

    private void ParsePath(ReadOnlySpan<Char> span, Int32 p)
    {
        // 路径后面可能跟着查询参数
        var queryIndex = span[p..].IndexOf('?');
        if (queryIndex >= 0)
        {
            queryIndex += p;
            AbsolutePath = span[p..queryIndex].ToString();
            Query = span[queryIndex..].ToString();
        }
        else
        {
            AbsolutePath = span[p..].ToString();
        }
    }

    private void ParseHost(ReadOnlySpan<Char> span)
    {
        // 拆分主机和端口，注意IPv6地址用方括号包裹
        if (span.Length <= 0) return;

        // 检查是否是 IPv6 地址（以 [ 开头）
        if (span[0] == '[')
        {
            var closeBracketIndex = span.IndexOf(']');
            if (closeBracketIndex > 0)
            {
                // IPv6 地址：[host]:port 或 [host]，去掉方括号保存
                Host = span[1..closeBracketIndex].ToString();

                // 检查是否有端口
                if (closeBracketIndex + 1 < span.Length && span[closeBracketIndex + 1] == ':')
                {
                    Port = span[(closeBracketIndex + 2)..].ToString().ToInt();
                }
            }
            else
            {
                // 格式错误的 IPv6，只有左方括号没有右方括号，去掉左方括号后当作主机
                Host = span[1..].ToString();
            }
        }
        else
        {
            // 普通主机名或 IPv4：host:port
            var colonIndex = span.LastIndexOf(':');
            if (colonIndex > 0)
            {
                Host = span[..colonIndex].ToString();
                Port = span[(colonIndex + 1)..].ToString().ToInt();
            }
            else
            {
                Host = span.ToString();
            }
        }
    }

    /// <summary>拼接请求参数</summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public UriInfo Append(String name, Object? value)
    {
        var str = HttpUtility.UrlEncode(value + "");

        var q = Query;
        Query = q.IsNullOrEmpty() ? $"{name}={str}" : $"{q}&{name}={str}";

        return this;
    }

    /// <summary>拼接请求参数（非空）</summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public UriInfo AppendNotEmpty(String name, Object? value)
    {
        if (value == null) return this;

        var str = value + "";
        if (str.IsNullOrEmpty()) return this;
        str = HttpUtility.UrlEncode(str);

        var q = Query;
        Query = q.IsNullOrEmpty() ? $"{name}={str}" : $"{q}&{name}={str}";

        return this;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String? ToString()
    {
        var authority = Authority;
        if (Scheme.IsNullOrEmpty())
        {
            if (authority.IsNullOrEmpty()) return PathAndQuery;

            return $"{authority}{PathAndQuery}";
        }

        return $"{Scheme}://{authority}{PathAndQuery}";
    }
    #endregion
}
