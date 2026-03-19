using System.Collections.Concurrent;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Remoting;

/// <summary>服务解析器。根据服务名获取通信客户端，用于服务发现场景</summary>
/// <remarks>
/// 三层架构设计：
/// <list type="number">
/// <item><description>NewLife.Core 定义接口与基于配置的默认实现 <see cref="ConfigServiceResolver"/>，仅支持 http/https</description></item>
/// <item><description>NewLife.Remoting 提供 RemotingServiceResolver，扩展支持 tcp/udp/ws/wss 等长连接协议</description></item>
/// <item><description>Stardust 提供完整实现（通过 IRegistry），从注册中心动态发现服务地址并支持权重、自动更新</description></item>
/// </list>
/// 在类库项目中仅引用 NewLife.Core 即可通过 DI 获取该接口，
/// 由上层应用（如 Stardust）提供具体实现，支持注册中心的服务发现与动态地址更新。
/// </remarks>
public interface IServiceResolver
{
    /// <summary>为指定服务获取客户端，自动从注册中心订阅服务地址并支持动态更新</summary>
    /// <remarks>同名服务（含 tag）复用同一实例，推荐在应用生命周期内长持客户端</remarks>
    /// <param name="serviceName">服务名。用于在配置中心或注册中心定位服务地址</param>
    /// <param name="tag">特性标签。用于区分同一服务的不同环境或分组，为空时不区分</param>
    /// <returns>返回与服务通信的客户端实例</returns>
    Task<IApiClient> GetClientAsync(String serviceName, String? tag = null);

    /// <summary>解析服务的地址列表，供调用方自行创建客户端</summary>
    /// <remarks>
    /// 与 <see cref="GetClientAsync"/> 的区别：
    /// <list type="bullet">
    /// <item><description><see cref="GetClientAsync"/> 返回托管客户端（内置负载均衡、故障转移、自动更新）</description></item>
    /// <item><description><see cref="ResolveAddressesAsync"/> 仅返回地址字符串，适合需要自定义协议或客户端类型的场景</description></item>
    /// </list>
    /// </remarks>
    /// <param name="serviceName">服务名</param>
    /// <param name="tag">特性标签。为空时返回所有地址</param>
    /// <returns>地址列表，未找到时返回空数组</returns>
    Task<String[]> ResolveAddressesAsync(String serviceName, String? tag = null);
}

/// <summary>基于配置的服务解析器。从配置中心或本地配置文件读取服务地址，仅支持 http/https 协议</summary>
/// <remarks>
/// 以服务名为键从 <see cref="IConfigProvider"/> 读取地址（优先 DI 中的配置提供者，其次本地 appsettings.json），
/// 支持逗号或分号分隔的多地址（轮询负载均衡）。
/// 同名服务（含 tag）复用同一 <see cref="IApiClient"/> 实例。
/// 子类可重写 <see cref="BuildClient"/> 以支持更多协议（如 NewLife.Remoting 中的 RemotingServiceResolver）。
/// <para>地址解析优先级：<see cref="Servers"/> 属性 → DI 中的 IConfigProvider → 本地 appsettings.json</para>
/// </remarks>
public class ConfigServiceResolver : DisposeBase, IServiceResolver
{
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>实例化，指定服务提供者，用于解析 IConfigProvider、ITracer、ILog 等依赖</summary>
    /// <param name="serviceProvider">服务提供者（DI 容器）</param>
    public ConfigServiceResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>实例化，指定配置提供者，直接使用指定的配置源读取服务地址</summary>
    /// <param name="configProvider">配置提供者</param>
    public ConfigServiceResolver(IConfigProvider configProvider)
    {
        _configProvider = configProvider;
    }

    /// <summary>服务地址。直接指定所有服务统一使用的地址，优先级高于配置文件，支持逗号或分号分隔的多地址</summary>
    public String? Servers { get; set; }

    private IConfigProvider? _configProvider;
    private readonly ConcurrentDictionary<String, IApiClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>销毁，释放已缓存的所有客户端</summary>
    /// <param name="disposing">true 表示从 Dispose 调用；false 表示终结器调用</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        foreach (var client in _clients.Values)
        {
            client.TryDispose();
        }
        _clients.Clear();
    }

    /// <summary>为指定服务获取客户端，同名服务（含 tag）复用同一实例</summary>
    /// <param name="serviceName">服务名。不能为空</param>
    /// <param name="tag">特性标签。用于区分同一服务的不同分组</param>
    /// <returns>与服务通信的 IApiClient 实例</returns>
    public virtual Task<IApiClient> GetClientAsync(String serviceName, String? tag = null)
    {
        if (serviceName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(serviceName));

        var key = tag.IsNullOrEmpty() ? serviceName : $"{serviceName}#{tag}";
        return Task.FromResult(_clients.GetOrAdd(key, _ => CreateClient(serviceName, tag)));
    }

    /// <summary>解析服务的地址列表，供调用方自行创建客户端</summary>
    /// <param name="serviceName">服务名。不能为空</param>
    /// <param name="tag">特性标签（本实现不使用，由注册中心实现用于过滤）</param>
    /// <returns>地址列表，未找到时返回空数组</returns>
    public virtual Task<String[]> ResolveAddressesAsync(String serviceName, String? tag = null)
    {
        if (serviceName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(serviceName));

        var address = ReadAddress(serviceName);
        if (address.IsNullOrEmpty()) return Task.FromResult(new String[0]);

        var addrs = address.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < addrs.Length; i++)
        {
            addrs[i] = addrs[i].Trim();
        }
        return Task.FromResult(addrs);
    }

    /// <summary>根据服务名读取地址字符串。按优先级：Servers 属性 → IConfigProvider → appsettings.json</summary>
    /// <param name="serviceName">服务名</param>
    /// <returns>地址字符串，未找到时返回 null</returns>
    private String? ReadAddress(String serviceName)
    {
        var address = Servers;
        if (!address.IsNullOrEmpty()) return address;

        var config = _configProvider;
        config ??= _serviceProvider?.GetService<IConfigProvider>();
        config ??= JsonConfigProvider.LoadAppSettings(null);
        return config?[serviceName];
    }

    /// <summary>根据服务名创建客户端。按优先级读取地址配置并调用 BuildClient</summary>
    /// <remarks>
    /// 地址解析优先级：
    /// <list type="number">
    /// <item><description><see cref="Servers"/> 属性（直接指定）</description></item>
    /// <item><description>DI 容器中的 <see cref="IConfigProvider"/></description></item>
    /// <item><description>本地 appsettings.json 文件</description></item>
    /// </list>
    /// 当地址来自配置（非 <see cref="Servers"/> 属性）时，自动将 <see cref="IConfigProvider"/> 绑定到客户端，
    /// 配置变更时客户端服务地址随之自动更新。
    /// </remarks>
    /// <param name="serviceName">服务名</param>
    /// <param name="tag">特性标签</param>
    /// <returns>新建的 IApiClient 实例</returns>
    protected virtual IApiClient CreateClient(String serviceName, String? tag)
    {
        var address = ReadAddress(serviceName);
        if (address.IsNullOrEmpty()) throw new InvalidOperationException($"配置中未找到服务[{serviceName}]的地址");

        var client = BuildClient(serviceName, address);

        // 地址来自配置时绑定 IConfigProvider，配置变更时自动更新客户端地址
        // Servers 属性为静态指定，无需绑定
        if (Servers.IsNullOrEmpty())
        {
            var config = _configProvider ?? _serviceProvider?.GetService<IConfigProvider>();
            if (config != null && client is IConfigMapping map)
                config.Bind(map, true, serviceName);
        }

        return client;
    }

    /// <summary>根据地址字符串构建客户端。仅支持 http/https，子类可重写以支持更多协议</summary>
    /// <param name="name">服务名，用作节点标识</param>
    /// <param name="address">地址，支持逗号或分号分隔的多地址</param>
    /// <returns>构建的 ApiHttpClient 实例</returns>
    protected virtual IApiClient BuildClient(String name, String address)
    {
        var addrs = address.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
        if (addrs.Length == 0) throw new InvalidOperationException($"服务[{name}]地址不能为空");

        foreach (var addr in addrs)
        {
            var a = addr.Trim();
            if (!a.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !a.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"服务[{name}]地址协议不支持（仅支持 http/https）：{a}");
        }

        var http = new ApiHttpClient
        {
            LoadBalanceMode = LoadBalanceMode.RoundRobin,
            ServiceProvider = _serviceProvider,
            Tracer = _serviceProvider?.GetService<ITracer>(),
        };
        var log = _serviceProvider?.GetService<ILog>();
        if (log != null) http.Log = log;

        // AddServer 仅支持逗号分隔，统一替换分号
        http.AddServer(name, address.Replace(';', ','));

        return http;
    }
}
