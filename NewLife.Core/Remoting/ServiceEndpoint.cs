using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NewLife.Remoting;

public partial class ApiHttpClient
{
    /// <summary>服务项。向后兼容的类型别名</summary>
    [Obsolete("请使用 HttpServiceNode")]
    public class Service : ServiceEndpoint { }
}

/// <summary>端点类型。用于竞速时的优先级排序</summary>
public enum EndpointCategory
{
    /// <summary>内网IPv4</summary>
    InternalIPv4,
    /// <summary>内网IPv6</summary>
    InternalIPv6,
    /// <summary>公网IPv4</summary>
    ExternalIPv4,
    /// <summary>公网域名</summary>
    ExternalDomain,
    /// <summary>公网IPv6</summary>
    ExternalIPv6
}

/// <summary>Http服务节点</summary>
/// <remarks>
/// 表示一个可用于负载均衡和故障转移的Http服务端点。
/// 包含服务地址、权重、健康状态等信息，支持：
/// <list type="bullet">
/// <item><description>负载均衡：通过 <see cref="Weight"/> 属性控制流量分配比例</description></item>
/// <item><description>故障转移：通过 <see cref="NextTime"/> 属性实现节点屏蔽和恢复</description></item>
/// <item><description>健康监控：通过 <see cref="Times"/>、<see cref="Errors"/> 统计请求状态</description></item>
/// <item><description>连接复用：通过 <see cref="Client"/> 属性复用 HttpClient 实例</description></item>
/// <item><description>竞速调用：通过 <see cref="Rtt"/>、<see cref="Category"/> 实现智能排序</description></item>
/// </list>
/// </remarks>
public class ServiceEndpoint
{
    #region 属性
    /// <summary>名称。用于标识和日志记录</summary>
    public String Name { get; set; } = null!;

    /// <summary>URI名称。地址的规范化表示，如 http://127.0.0.1:8080</summary>
    public String UriName { get; set; } = null!;

    /// <summary>服务地址</summary>
    public Uri Address { get; set; } = null!;

    /// <summary>访问令牌。用于该节点的身份验证</summary>
    public String? Token { get; set; }

    /// <summary>总请求次数</summary>
    public Int32 Times { get; set; }

    /// <summary>错误次数</summary>
    public Int32 Errors { get; set; }

    /// <summary>创建时间。HttpClient 创建时间，每过一段时间清空重建，更新域名缓存</summary>
    [XmlIgnore, IgnoreDataMember]
    public DateTime CreateTime { get; set; }

    /// <summary>下次可用时间。节点出错时设置屏蔽期，在此时间之前不会被选中</summary>
    [XmlIgnore, IgnoreDataMember]
    public DateTime NextTime { get; set; }

    /// <summary>Http客户端。复用连接以提高性能</summary>
    [XmlIgnore, IgnoreDataMember]
    public HttpClient? Client { get; set; }
    #endregion

    #region 加权轮询属性
    /// <summary>权重。用于加权轮询负载均衡，默认1，值越大分配的请求越多</summary>
    public Int32 Weight { get; set; } = 1;

    /// <summary>轮询索引。加权轮询时记录当前轮次的使用次数</summary>
    internal Int32 Index;
    #endregion

    #region 竞速属性
    /// <summary>端点类别。用于竞速时的优先级排序</summary>
    [XmlIgnore, IgnoreDataMember]
    public EndpointCategory Category { get; set; }

    /// <summary>往返耗时。用于竞速时的延迟排序</summary>
    [XmlIgnore, IgnoreDataMember]
    public TimeSpan? Rtt { get; set; }

    /// <summary>最后成功时间</summary>
    [XmlIgnore, IgnoreDataMember]
    public DateTime LastSuccess { get; set; }

    /// <summary>最后失败时间</summary>
    [XmlIgnore, IgnoreDataMember]
    public DateTime LastFailure { get; set; }

    /// <summary>下一次探测时间</summary>
    [XmlIgnore, IgnoreDataMember]
    public DateTime NextProbe { get; set; }

    /// <summary>延迟分数。用于竞速时的启动延迟（毫秒）</summary>
    [XmlIgnore, IgnoreDataMember]
    public Int32 Score { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化服务节点</summary>
    public ServiceEndpoint() { }

    /// <summary>实例化服务节点</summary>
    /// <param name="name">名称</param>
    /// <param name="address">服务地址</param>
    public ServiceEndpoint(String name, Uri address)
    {
        Name = name;
        SetAddress(address);
    }

    /// <summary>实例化服务节点</summary>
    /// <param name="name">名称</param>
    /// <param name="address">服务地址</param>
    public ServiceEndpoint(String name, String address) : this(name, new Uri(address)) { }
    #endregion

    #region 方法
    /// <summary>设置地址。同时生成UriName并设置端点类别</summary>
    /// <param name="uri">地址</param>
    /// <param name="internalAddress">是否内网地址</param>
    public void SetAddress(Uri uri, Boolean? internalAddress = null)
    {
        if (uri == null) return;

        Address = uri;
        UriName = GetUriName(uri);
        if (Name.IsNullOrEmpty()) Name = UriName;

        // 设置端点类别
        internalAddress ??= !Name.IsNullOrEmpty() && (Name.Contains("内网") || Name.Contains("Internal", StringComparison.OrdinalIgnoreCase));
        Category = InferCategory(uri, internalAddress ?? false);
    }

    /// <summary>推断端点类别</summary>
    /// <param name="uri">地址</param>
    /// <param name="internalAddress">是否内网地址</param>
    /// <returns></returns>
    private static EndpointCategory InferCategory(Uri uri, Boolean internalAddress)
    {
        if (!IPAddress.TryParse(uri.Host, out var ip))
            return EndpointCategory.ExternalDomain;

#if NETCOREAPP || NETSTANDARD2_1
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
#endif

        if (ip.AddressFamily == AddressFamily.InterNetwork)
            return internalAddress ? EndpointCategory.InternalIPv4 : EndpointCategory.ExternalIPv4;

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return internalAddress ? EndpointCategory.InternalIPv6 : EndpointCategory.ExternalIPv6;

        return EndpointCategory.ExternalDomain;
    }

    /// <summary>重置节点状态，清除屏蔽</summary>
    public void Reset()
    {
        NextTime = DateTime.MinValue;
        Client = null;
        CreateTime = DateTime.MinValue;
    }

    /// <summary>标记节点失败，设置屏蔽期</summary>
    /// <param name="shieldingSeconds">屏蔽时间（秒）</param>
    public void MarkFailure(Int32 shieldingSeconds)
    {
        Errors++;
        Client = null;
        NextTime = DateTime.Now.AddSeconds(shieldingSeconds);
        CreateTime = DateTime.MinValue;
    }

    /// <summary>检查节点是否可用</summary>
    /// <returns>如果节点未被屏蔽则返回true</returns>
    public Boolean IsAvailable() => NextTime < DateTime.Now;
    #endregion

    #region 辅助
    /// <summary>获取服务基地址名称</summary>
    /// <remarks>
    /// 返回URI的权威部分（scheme + host + port），并去除末尾“/”。
    /// 常用于生成可用于比较/分组的规范化地址，如："http://127.0.0.1:8080"。
    /// </remarks>
    /// <param name="uri">服务地址</param>
    /// <returns>规范化后的服务基地址名称</returns>
    public static String GetUriName(Uri uri) => uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');

    /// <summary>已重载。友好显示</summary>
    /// <returns></returns>
    public override String ToString() => $"{Name} {Address}";
    #endregion
}
