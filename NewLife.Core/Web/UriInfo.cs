namespace NewLife.Web;

/// <summary>资源定位。无限制解析Url地址</summary>
public class UriInfo
{
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
    public String? PathAndQuery => AbsolutePath + Query;

    /// <summary>实例化</summary>
    /// <param name="value"></param>
    public UriInfo(String value) => Parse(value);

    /// <summary>主机与端口。省略默认端口</summary>
    public String? Authority
    {
        get
        {
            if (Port == 0) return Host;
            if (Host.IsNullOrEmpty()) return Host;

            if (Scheme.EqualIgnoreCase("http", "ws"))
                return Port == 80 ? Host : $"{Host}:{Port}";
            else if (Scheme.EqualIgnoreCase("https", "wss"))
                return Port == 443 ? Host : $"{Host}:{Port}";

            return $"{Host}:{Port}";
        }
    }

    /// <summary>解析Url字符串</summary>
    /// <param name="value"></param>
    public void Parse(String value)
    {
        if (value.IsNullOrWhiteSpace()) return;

        // 先处理头尾，再处理中间的主机和端口
        var p = value.IndexOf("://");
        if (p >= 0)
        {
            Scheme = value[..p];
            p += 3;
        }
        else
            p = 0;

        // 第二步找到/，它左边是主机和端口，右边是路径和查询。如果没有/，则整个字符串都是主机和端口
        var p2 = value.IndexOf('/', p);
        if (p2 >= 0)
        {
            ParseHost(value[p..p2]);
            ParsePath(value, p2);
        }
        else
        {
            p2 = value.IndexOf('?', p);
            if (p2 >= 0)
            {
                ParseHost(value[p..p2]);
                Query = value[p2..];
            }
            else
            {
                Host = value[p..];
            }
        }

        if (AbsolutePath.IsNullOrEmpty()) AbsolutePath = "/";
    }

    private void ParsePath(String value, Int32 p)
    {
        // 第二步找到/，它左边是主机和端口，右边是路径和查询。如果没有/，则整个字符串都是主机和端口
        var p2 = value.IndexOf('?', p);
        if (p2 >= 0)
        {
            AbsolutePath = value[p..p2];
            Query = value[p2..];
        }
        else
        {
            AbsolutePath = value[p..];
        }
    }

    private void ParseHost(String value)
    {
        // 拆分主机和端口，注意IPv6地址
        var p2 = value.LastIndexOf(':');
        if (p2 > 0)
        {
            Host = value[..p2];
            Port = value[(p2 + 1)..].ToInt();
        }
        else if (!value.IsNullOrEmpty())
        {
            Host = value;
        }
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
}
