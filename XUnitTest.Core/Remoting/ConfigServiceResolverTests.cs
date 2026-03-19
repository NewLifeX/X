using NewLife;
using NewLife.Configuration;
using NewLife.Model;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Remoting;

/// <summary>ConfigServiceResolver 单元测试</summary>
public class ConfigServiceResolverTests : IDisposable
{
    private ConfigServiceResolver? _resolver;

    public void Dispose() => _resolver?.Dispose();

    #region GetClientAsync
    [Fact(DisplayName = "指定Servers地址时应返回ApiHttpClient")]
    public async Task GetClientAsync_WithServers_ReturnsClient()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        var client = await _resolver.GetClientAsync("TestService");

        Assert.NotNull(client);
        Assert.IsType<ApiHttpClient>(client);
    }

    [Fact(DisplayName = "同名服务应复用同一客户端实例")]
    public async Task GetClientAsync_SameName_ReturnsSameInstance()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        var client1 = await _resolver.GetClientAsync("TestService");
        var client2 = await _resolver.GetClientAsync("TestService");

        Assert.Same(client1, client2);
    }

    [Fact(DisplayName = "不同Tag应返回不同客户端实例")]
    public async Task GetClientAsync_DifferentTag_ReturnsDifferentInstance()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        var client1 = await _resolver.GetClientAsync("TestService", "dev");
        var client2 = await _resolver.GetClientAsync("TestService", "prod");

        Assert.NotSame(client1, client2);
    }

    [Fact(DisplayName = "相同Tag应复用同一客户端实例")]
    public async Task GetClientAsync_SameTag_ReturnsSameInstance()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        var client1 = await _resolver.GetClientAsync("TestService", "dev");
        var client2 = await _resolver.GetClientAsync("TestService", "dev");

        Assert.Same(client1, client2);
    }

    [Fact(DisplayName = "无Tag和空Tag应返回同一实例")]
    public async Task GetClientAsync_NullAndEmptyTag_ReturnsSameInstance()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        var client1 = await _resolver.GetClientAsync("TestService", null);
        var client2 = await _resolver.GetClientAsync("TestService", "");

        Assert.Same(client1, client2);
    }

    [Fact(DisplayName = "服务名为空时应抛出ArgumentNullException")]
    public async Task GetClientAsync_EmptyServiceName_ThrowsArgumentNull()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        await Assert.ThrowsAsync<ArgumentNullException>(() => _resolver.GetClientAsync(""));
    }

    [Fact(DisplayName = "服务名为null时应抛出ArgumentNullException")]
    public async Task GetClientAsync_NullServiceName_ThrowsArgumentNull()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        await Assert.ThrowsAsync<ArgumentNullException>(() => _resolver.GetClientAsync(null!));
    }
    #endregion

    #region 多地址
    [Fact(DisplayName = "逗号分隔的多地址应正确构建客户端")]
    public async Task GetClientAsync_MultipleAddresses_Comma()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080,http://127.0.0.1:8081"
        };

        var client = await _resolver.GetClientAsync("TestService");

        Assert.NotNull(client);
        var http = Assert.IsType<ApiHttpClient>(client);
        Assert.Equal(2, http.Services.Count);
    }

    [Fact(DisplayName = "分号分隔的多地址应正确构建客户端")]
    public async Task GetClientAsync_MultipleAddresses_Semicolon()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080;http://127.0.0.1:8081"
        };

        var client = await _resolver.GetClientAsync("TestService");

        Assert.NotNull(client);
        var http = Assert.IsType<ApiHttpClient>(client);
        Assert.Equal(2, http.Services.Count);
    }
    #endregion

    #region 协议校验
    [Fact(DisplayName = "不支持的协议应抛出NotSupportedException")]
    public async Task GetClientAsync_UnsupportedProtocol_ThrowsNotSupported()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "tcp://127.0.0.1:8080"
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => _resolver.GetClientAsync("TestService"));
    }

    [Fact(DisplayName = "https地址应正常构建客户端")]
    public async Task GetClientAsync_HttpsAddress_ReturnsClient()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "https://api.example.com"
        };

        var client = await _resolver.GetClientAsync("TestService");

        Assert.NotNull(client);
        Assert.IsType<ApiHttpClient>(client);
    }

    [Fact(DisplayName = "混合http和非http地址应抛出NotSupportedException")]
    public async Task GetClientAsync_MixedProtocols_ThrowsNotSupported()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080,tcp://127.0.0.1:9090"
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => _resolver.GetClientAsync("TestService"));
    }
    #endregion

    #region 配置读取
    [Fact(DisplayName = "从IConfigProvider读取服务地址")]
    public async Task GetClientAsync_FromConfigProvider()
    {
        var config = new DictionaryConfigProvider();
        config["OrderService"] = "http://127.0.0.1:9001";

        _resolver = new ConfigServiceResolver(config);

        var client = await _resolver.GetClientAsync("OrderService");

        Assert.NotNull(client);
        Assert.IsType<ApiHttpClient>(client);
    }

    [Fact(DisplayName = "配置中找不到地址时应抛出InvalidOperationException")]
    public async Task GetClientAsync_NoConfig_ThrowsInvalidOperation()
    {
        var config = new DictionaryConfigProvider();
        _resolver = new ConfigServiceResolver(config);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _resolver.GetClientAsync("NonExistService"));
        Assert.Contains("NonExistService", ex.Message);
    }

    [Fact(DisplayName = "Servers属性优先于配置提供者")]
    public async Task GetClientAsync_ServersTakesPriority()
    {
        var config = new DictionaryConfigProvider();
        config["TestService"] = "http://config-addr:9001";

        var ioc = new ObjectContainer();
        ioc.AddSingleton<IConfigProvider>(config);
        var sp = ioc.BuildServiceProvider();

        _resolver = new ConfigServiceResolver(sp)
        {
            Servers = "http://override-addr:8080"
        };

        var client = await _resolver.GetClientAsync("TestService");
        var http = Assert.IsType<ApiHttpClient>(client);
        // Servers 指定的地址应被使用
        Assert.True(http.Services.Count > 0);
        Assert.Contains("override-addr", http.Services[0].Address?.ToString());
    }
    #endregion

    #region Dispose
    [Fact(DisplayName = "Dispose应释放所有缓存的客户端")]
    public async Task Dispose_CleansUpClients()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        var client = await _resolver.GetClientAsync("TestService");
        Assert.NotNull(client);

        _resolver.Dispose();
        Assert.True(_resolver.Disposed);

        // 再次调用应不会报错（DisposeBase 幂等）
        _resolver.Dispose();
        _resolver = null; // 避免 IDisposable.Dispose 再次调用
    }
    #endregion

    #region 负载均衡模式
    [Fact(DisplayName = "创建的客户端应使用RoundRobin负载均衡")]
    public async Task GetClientAsync_UsesRoundRobinLoadBalance()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080,http://127.0.0.1:8081"
        };

        var client = await _resolver.GetClientAsync("TestService");
        var http = Assert.IsType<ApiHttpClient>(client);

        Assert.Equal(LoadBalanceMode.RoundRobin, http.LoadBalanceMode);
    }
    #endregion

    #region IConfigProvider构造函数
    [Fact(DisplayName = "使用IConfigProvider构造函数应能正确解析地址")]
    public async Task Constructor_WithConfigProvider_ResolvesAddress()
    {
        var config = new DictionaryConfigProvider();
        config["PayService"] = "http://pay.example.com:8080";

        _resolver = new ConfigServiceResolver(config);

        var client = await _resolver.GetClientAsync("PayService");

        Assert.NotNull(client);
        var http = Assert.IsType<ApiHttpClient>(client);
        Assert.Single(http.Services);
    }
    #endregion

    #region ResolveAddressesAsync
    [Fact(DisplayName = "ResolveAddressesAsync单地址应返回一个地址")]
    public async Task ResolveAddressesAsync_SingleAddress_ReturnsOneAddress()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        var addrs = await _resolver.ResolveAddressesAsync("TestService");

        Assert.Single(addrs);
        Assert.Equal("http://127.0.0.1:8080", addrs[0]);
    }

    [Fact(DisplayName = "ResolveAddressesAsync逗号多地址应返回多个地址")]
    public async Task ResolveAddressesAsync_MultipleAddresses_ReturnsMultipleAddresses()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080,http://127.0.0.1:8081,http://127.0.0.1:8082"
        };

        var addrs = await _resolver.ResolveAddressesAsync("TestService");

        Assert.Equal(3, addrs.Length);
        Assert.Equal("http://127.0.0.1:8080", addrs[0]);
        Assert.Equal("http://127.0.0.1:8081", addrs[1]);
        Assert.Equal("http://127.0.0.1:8082", addrs[2]);
    }

    [Fact(DisplayName = "ResolveAddressesAsync分号多地址应返回多个地址")]
    public async Task ResolveAddressesAsync_SemicolonAddresses_ReturnsMultipleAddresses()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080;http://127.0.0.1:8081"
        };

        var addrs = await _resolver.ResolveAddressesAsync("TestService");

        Assert.Equal(2, addrs.Length);
        Assert.Equal("http://127.0.0.1:8080", addrs[0]);
        Assert.Equal("http://127.0.0.1:8081", addrs[1]);
    }

    [Fact(DisplayName = "ResolveAddressesAsync未配置地址应返回空数组")]
    public async Task ResolveAddressesAsync_NoConfig_ReturnsEmpty()
    {
        var config = new DictionaryConfigProvider();
        _resolver = new ConfigServiceResolver(config);

        var addrs = await _resolver.ResolveAddressesAsync("NonExistService");

        Assert.Empty(addrs);
    }

    [Fact(DisplayName = "ResolveAddressesAsync地址应去除首尾空白")]
    public async Task ResolveAddressesAsync_AddressTrimmed()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = " http://127.0.0.1:8080 , http://127.0.0.1:8081 "
        };

        var addrs = await _resolver.ResolveAddressesAsync("TestService");

        Assert.Equal(2, addrs.Length);
        Assert.Equal("http://127.0.0.1:8080", addrs[0]);
        Assert.Equal("http://127.0.0.1:8081", addrs[1]);
    }

    [Fact(DisplayName = "ResolveAddressesAsync服务名为空时应抛出ArgumentNullException")]
    public async Task ResolveAddressesAsync_EmptyServiceName_ThrowsArgumentNull()
    {
        var ioc = new ObjectContainer();
        _resolver = new ConfigServiceResolver(ioc.BuildServiceProvider())
        {
            Servers = "http://127.0.0.1:8080"
        };

        await Assert.ThrowsAsync<ArgumentNullException>(() => _resolver.ResolveAddressesAsync(""));
    }

    [Fact(DisplayName = "ResolveAddressesAsync从IConfigProvider读取地址")]
    public async Task ResolveAddressesAsync_FromConfigProvider_ReturnsAddresses()
    {
        var config = new DictionaryConfigProvider();
        config["UserService"] = "http://user1:8080,http://user2:8080";

        _resolver = new ConfigServiceResolver(config);

        var addrs = await _resolver.ResolveAddressesAsync("UserService");

        Assert.Equal(2, addrs.Length);
        Assert.Equal("http://user1:8080", addrs[0]);
        Assert.Equal("http://user2:8080", addrs[1]);
    }
    #endregion

    #region IConfigProvider绑定
    [Fact(DisplayName = "GetClientAsync从配置读取地址时应绑定IConfigProvider")]
    public async Task GetClientAsync_FromConfig_BindsConfigProvider()
    {
        var config = new DictionaryConfigProvider();
        config["OrderService"] = "http://192.168.1.100:8080";
        _resolver = new ConfigServiceResolver(config);

        await _resolver.GetClientAsync("OrderService");

        Assert.Equal(1, config.BindCallCount);
    }

    [Fact(DisplayName = "GetClientAsync使用Servers属性时不应绑定IConfigProvider")]
    public async Task GetClientAsync_WithServers_DoesNotBindConfigProvider()
    {
        var config = new DictionaryConfigProvider();
        _resolver = new ConfigServiceResolver(config)
        {
            Servers = "http://192.168.1.100:8080"
        };

        await _resolver.GetClientAsync("OrderService");

        Assert.Equal(0, config.BindCallCount);
    }

    [Fact(DisplayName = "GetClientAsync多次获取同一服务仅绑定一次")]
    public async Task GetClientAsync_SameService_BindsOnce()
    {
        var config = new DictionaryConfigProvider();
        config["PayService"] = "http://192.168.1.100:8080";
        _resolver = new ConfigServiceResolver(config);

        await _resolver.GetClientAsync("PayService");
        await _resolver.GetClientAsync("PayService");
        await _resolver.GetClientAsync("PayService");

        Assert.Equal(1, config.BindCallCount);
    }
    #endregion

    #region 辅助
    /// <summary>基于字典的简单配置提供者，用于测试</summary>
    private class DictionaryConfigProvider : IConfigProvider
    {
        private readonly Dictionary<String, String?> _data = new(StringComparer.OrdinalIgnoreCase);

        public String Name { get; set; } = "Test";
        public IConfigSection Root { get; set; } = new ConfigSection();
        public ICollection<String> Keys => _data.Keys;
        public Boolean IsNew { get; set; }

        public String? this[String key]
        {
            get => _data.TryGetValue(key, out var v) ? v : null;
            set => _data[key!] = value;
        }

        public event EventHandler? Changed;

        public GetConfigCallback GetConfig => key => this[key];
        public IConfigSection? GetSection(String key) => null;
        public Boolean LoadAll() => true;
        public Boolean SaveAll() => true;
        public T? Load<T>(String? path = null) where T : new() => default;
        public Boolean Save<T>(T model, String? path = null) => true;
        public Int32 BindCallCount { get; private set; }

        public void Bind<T>(T model, Boolean autoReload = true, String? path = null) => BindCallCount++;
        public void Bind<T>(T model, String path, Action<IConfigSection> onChange) { }
    }
    #endregion
}
