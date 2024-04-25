namespace NewLife.Web;

/// <summary>资源定位。无限制解析Url地址</summary>
public class UriInfo
{
    /// <summary>协议</summary>
    public String? Scheme { get; set; }

    /// <summary>协议</summary>
    public String Host { get; set; } = null!;

    /// <summary>协议</summary>
    public Int32 Port { get; set; }

    /// <summary>协议</summary>
    public String? PathAndQuery { get; set; }

    /// <summary>实例化</summary>
    /// <param name="value"></param>
    public UriInfo(String value) => Parse(value);

    /// <summary>主机与端口。省略默认端口</summary>
    public String Authority
    {
        get
        {
            if (Port == 0) return Host;

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

        var p2 = value.IndexOf('/', p);
        if (p2 > 0)
        {
            PathAndQuery = value[p2..];
            value = value[p..p2];
        }
        else
            value = value[p..];

        // 拆分主机和端口，注意IPv6地址
        p2 = value.LastIndexOf(':');
        if (p2 > 0)
        {
            Host = value[..p2];
            Port = value[(p2 + 1)..].ToInt();
        }
        else
        {
            Host = value;
        }
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{Scheme}://{Authority}{PathAndQuery}";
}
