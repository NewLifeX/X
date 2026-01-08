using System.Threading.Tasks;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Remoting;

/// <summary>负载均衡器测试</summary>
public class LoadBalancerTests
{
    #region 故障转移测试
    [Fact(DisplayName = "故障转移_优先使用主节点")]
    public void Failover_PreferPrimaryNode()
    {
        var services = CreateServices(3);
        var lb = new FailoverLoadBalancer();

        // 连续获取应该都是第一个节点
        for (var i = 0; i < 5; i++)
        {
            var svc = lb.GetService(services);
            Assert.Equal("service1", svc.Name);
        }
    }

    [Fact(DisplayName = "故障转移_主节点失败切换备用")]
    public void Failover_SwitchOnPrimaryFailure()
    {
        var services = CreateServices(3);
        var lb = new FailoverLoadBalancer { ShieldingTime = 60 };

        // 第一次获取主节点
        var svc1 = lb.GetService(services);
        Assert.Equal("service1", svc1.Name);

        // 模拟网络异常
        lb.PutService(services, svc1, new HttpRequestException("Connection refused"));

        // 应该切换到第二个节点
        var svc2 = lb.GetService(services);
        Assert.Equal("service2", svc2.Name);
    }

    [Fact(DisplayName = "故障转移_主节点恢复后切回")]
    public void Failover_RecoverToPrimary()
    {
        var services = CreateServices(3);
        var lb = new FailoverLoadBalancer { ShieldingTime = 1 }; // 1秒屏蔽时间

        // 模拟主节点失败
        var svc1 = lb.GetService(services);
        lb.PutService(services, svc1, new HttpRequestException("Connection refused"));

        // 切换到备用节点
        var svc2 = lb.GetService(services);
        Assert.Equal("service2", svc2.Name);

        // 等待屏蔽时间过去
        Thread.Sleep(1100);

        // 应该切回主节点
        var svc3 = lb.GetService(services);
        Assert.Equal("service1", svc3.Name);
    }

    [Fact(DisplayName = "故障转移_全部失败后重置")]
    public void Failover_ResetWhenAllFailed()
    {
        var services = CreateServices(2);
        var lb = new FailoverLoadBalancer { ShieldingTime = 60 };

        // 模拟所有节点失败
        foreach (var svc in services)
        {
            svc.NextTime = DateTime.Now.AddSeconds(60);
        }

        // 获取服务应该重置所有节点
        var result = lb.GetService(services);
        Assert.NotNull(result);

        // 所有节点应该被重置
        Assert.True(services.All(e => e.IsAvailable()));
    }
    #endregion

    #region 加权轮询测试
    [Fact(DisplayName = "加权轮询_按权重分配")]
    public void RoundRobin_DistributeByWeight()
    {
        var services = CreateServices(3);
        services[0].Weight = 3;
        services[1].Weight = 2;
        services[2].Weight = 1;

        var lb = new WeightedRoundRobinLoadBalancer();

        // 统计分配次数
        var counts = new Dictionary<String, Int32>();
        for (var i = 0; i < 12; i++)
        {
            var svc = lb.GetService(services);
            counts[svc.Name] = counts.GetValueOrDefault(svc.Name) + 1;
            lb.PutService(services, svc, null); // 成功归还
        }

        // 权重3的应该被分配更多次
        Assert.True(counts["service1"] >= counts["service2"]);
        Assert.True(counts["service2"] >= counts["service3"]);
    }

    [Fact(DisplayName = "加权轮询_跳过屏蔽节点")]
    public void RoundRobin_SkipShieldedNode()
    {
        var services = CreateServices(3);
        var lb = new WeightedRoundRobinLoadBalancer { ShieldingTime = 60 };

        // 屏蔽第一个节点
        services[0].NextTime = DateTime.Now.AddSeconds(60);

        // 应该跳过第一个节点
        for (var i = 0; i < 5; i++)
        {
            var svc = lb.GetService(services);
            Assert.NotEqual("service1", svc.Name);
        }
    }

    [Fact(DisplayName = "加权轮询_失败后屏蔽")]
    public void RoundRobin_ShieldOnFailure()
    {
        var services = CreateServices(2);
        var lb = new WeightedRoundRobinLoadBalancer { ShieldingTime = 60 };

        // 获取第一个节点并模拟失败
        var svc1 = lb.GetService(services);
        lb.PutService(services, svc1, new HttpRequestException("Connection refused"));

        // 第一个节点应该被屏蔽
        Assert.False(svc1.IsAvailable());

        // 后续应该使用第二个节点
        var svc2 = lb.GetService(services);
        Assert.Equal("service2", svc2.Name);
    }
    #endregion

    #region 竞速负载均衡测试
    [Fact(DisplayName = "竞速_获取所有可用服务")]
    public async Task Race_GetAllAvailableServices()
    {
        var services = CreateServices(3);
        var lb = new RaceLoadBalancer();

        var all = await lb.GetAllServicesAsync(services, false, default);

        Assert.Equal(3, all.Count);
        // 验证延迟递增（通过Score字段）
        for (var i = 0; i < all.Count - 1; i++)
        {
            Assert.True(all[i].Score <= all[i + 1].Score);
        }
    }

    [Fact(DisplayName = "竞速_跳过屏蔽节点")]
    public async Task Race_SkipShieldedNode()
    {
        var services = CreateServices(3);
        var lb = new RaceLoadBalancer();

        // 屏蔽第二个节点
        services[1].NextTime = DateTime.Now.AddSeconds(60);

        var all = await lb.GetAllServicesAsync(services, false, default);

        Assert.Equal(2, all.Count);
        Assert.DoesNotContain(all, e => e.Name == "service2");
    }

    [Fact(DisplayName = "竞速_按RTT排序")]
    public async Task Race_SortByRtt()
    {
        var services = CreateServices(3);
        var lb = new RaceLoadBalancer();

        // 标记不同的RTT
        lb.MarkSuccess(services[2], TimeSpan.FromMilliseconds(50));  // 最快
        lb.MarkSuccess(services[0], TimeSpan.FromMilliseconds(100));
        lb.MarkSuccess(services[1], TimeSpan.FromMilliseconds(200));

        var all = await lb.GetAllServicesAsync(services, false, default);

        // 验证按RTT排序：service3(50ms) < service1(100ms) < service2(200ms)
        Assert.Equal(3, all.Count);
        Assert.Equal("service3", all[0].Name);
        Assert.Equal("service1", all[1].Name);
        Assert.Equal("service2", all[2].Name);
    }

    [Fact(DisplayName = "竞速_按端点类别优先排序")]
    public async Task Race_SortByCategoryFirst()
    {
        var services = CreateServices(3);
        // 设置不同的端点类别
        services[0].Category = EndpointCategory.ExternalDomain;  // 外网域名
        services[1].Category = EndpointCategory.InternalIPv4;    // 内网IPv4（优先）
        services[2].Category = EndpointCategory.ExternalIPv4;    // 外网IPv4

        var lb = new RaceLoadBalancer();
        var all = await lb.GetAllServicesAsync(services, false, default);

        // 验证按类别排序：InternalIPv4 < ExternalIPv4 < ExternalDomain
        Assert.Equal(3, all.Count);
        Assert.Equal("service2", all[0].Name);  // InternalIPv4
        Assert.Equal("service3", all[1].Name);  // ExternalIPv4
        Assert.Equal("service1", all[2].Name);  // ExternalDomain
    }

    [Fact(DisplayName = "竞速_标记成功更新RTT")]
    public void Race_MarkSuccessUpdatesRtt()
    {
        var services = CreateServices(1);
        var lb = new RaceLoadBalancer();

        // 首次标记
        lb.MarkSuccess(services[0], TimeSpan.FromMilliseconds(100));
        Assert.Equal(TimeSpan.FromMilliseconds(100), services[0].Rtt);
        Assert.Equal(0, services[0].Errors);

        // 第二次标记，RTT平滑计算：(100*3 + 200)/4 = 125
        lb.MarkSuccess(services[0], TimeSpan.FromMilliseconds(200));
        Assert.Equal(TimeSpan.FromMilliseconds(125), services[0].Rtt);
    }

    [Fact(DisplayName = "竞速_标记失败清除RTT")]
    public void Race_MarkFailureClearsRtt()
    {
        var services = CreateServices(1);
        var lb = new RaceLoadBalancer();

        // 先标记成功
        lb.MarkSuccess(services[0], TimeSpan.FromMilliseconds(100));
        Assert.NotNull(services[0].Rtt);

        // 标记失败
        lb.MarkFailure(services[0], new Exception("test"));
        Assert.Null(services[0].Rtt);
        Assert.Equal(1, services[0].Errors);
    }

    [Fact(DisplayName = "竞速_Score字段正确设置")]
    public async Task Race_ScoreFieldCorrectlySet()
    {
        var services = CreateServices(3);
        var lb = new RaceLoadBalancer();

        var all = await lb.GetAllServicesAsync(services, false, default);

        // 验证Score按100递增
        Assert.Equal(0, all[0].Score);
        Assert.Equal(100, all[1].Score);
        Assert.Equal(200, all[2].Score);
    }
    #endregion

    #region ApiHttpClient集成测试
    [Fact(DisplayName = "ApiHttpClient_默认使用故障转移")]
    public void ApiHttpClient_DefaultFailover()
    {
        var client = new ApiHttpClient("http://127.0.0.1:10001,http://127.0.0.1:10002");

        Assert.Equal(LoadBalanceMode.Failover, client.LoadBalanceMode);
    }

    [Fact(DisplayName = "ApiHttpClient_切换到加权轮询")]
    public void ApiHttpClient_SwitchToRoundRobin()
    {
        var client = new ApiHttpClient("http://127.0.0.1:10001,http://127.0.0.1:10002");

        client.LoadBalanceMode = LoadBalanceMode.RoundRobin;

        Assert.Equal(LoadBalanceMode.RoundRobin, client.LoadBalanceMode);
    }

    [Fact(DisplayName = "ApiHttpClient_切换到竞速模式")]
    public void ApiHttpClient_SwitchToRace()
    {
        var client = new ApiHttpClient("http://127.0.0.1:10001,http://127.0.0.1:10002");

        client.LoadBalanceMode = LoadBalanceMode.Race;

        Assert.Equal(LoadBalanceMode.Race, client.LoadBalanceMode);
    }

    [Fact(DisplayName = "ApiHttpClient_兼容RoundRobin属性")]
    public void ApiHttpClient_RoundRobinPropertyCompatibility()
    {
        var client = new ApiHttpClient("http://127.0.0.1:10001,http://127.0.0.1:10002");

        // 测试设置
#pragma warning disable CS0618
        client.RoundRobin = true;
        Assert.Equal(LoadBalanceMode.RoundRobin, client.LoadBalanceMode);
        Assert.True(client.RoundRobin);

        client.RoundRobin = false;
        Assert.Equal(LoadBalanceMode.Failover, client.LoadBalanceMode);
        Assert.False(client.RoundRobin);
#pragma warning restore CS0618
    }

    [Fact(DisplayName = "ApiHttpClient_屏蔽时间设置")]
    public void ApiHttpClient_ShieldingTimeSettings()
    {
        var client = new ApiHttpClient("http://127.0.0.1:10001");

        client.ShieldingTime = 120;

        Assert.Equal(120, client.ShieldingTime);
    }

    [Fact(DisplayName = "ApiHttpClient_Service类型别名兼容")]
    public void ApiHttpClient_ServiceTypeAliasCompatibility()
    {
        // 测试旧的 ApiHttpClient.Service 类型别名仍然可用
#pragma warning disable CS0618
        var service = new ApiHttpClient.Service
        {
            Name = "test",
            Weight = 2
        };
        service.SetAddress(new Uri("http://127.0.0.1:8080"));
#pragma warning restore CS0618

        Assert.Equal("test", service.Name);
        Assert.Equal(2, service.Weight);
        Assert.Equal("http://127.0.0.1:8080", service.UriName);
    }
    #endregion

    #region HttpServiceNode测试
    [Fact(DisplayName = "HttpServiceNode_基本属性")]
    public void HttpServiceNode_BasicProperties()
    {
        var node = new ServiceEndpoint("test", "http://127.0.0.1:8080");

        Assert.Equal("test", node.Name);
        Assert.Equal("http://127.0.0.1:8080", node.UriName);
        Assert.Equal(1, node.Weight);
        Assert.True(node.IsAvailable());
    }

    [Fact(DisplayName = "HttpServiceNode_标记失败")]
    public void HttpServiceNode_MarkFailure()
    {
        var node = new ServiceEndpoint("test", "http://127.0.0.1:8080");

        node.MarkFailure(60);

        Assert.Equal(1, node.Errors);
        Assert.False(node.IsAvailable());
        Assert.Null(node.Client);
    }

    [Fact(DisplayName = "HttpServiceNode_重置状态")]
    public void HttpServiceNode_Reset()
    {
        var node = new ServiceEndpoint("test", "http://127.0.0.1:8080");
        node.MarkFailure(60);

        node.Reset();

        Assert.True(node.IsAvailable());
        Assert.Null(node.Client);
    }

    [Fact(DisplayName = "HttpServiceNode_端点类别推断")]
    public void HttpServiceNode_CategoryInference()
    {
        // IPv4地址
        var node1 = new ServiceEndpoint("test1", "http://192.168.1.1:8080");
        Assert.Equal(EndpointCategory.ExternalIPv4, node1.Category);

        // 域名
        var node2 = new ServiceEndpoint("test2", "http://example.com:8080");
        Assert.Equal(EndpointCategory.ExternalDomain, node2.Category);

        // 使用SetAddress设置内网地址
        var node3 = new ServiceEndpoint { Name = "test3" };
        node3.SetAddress(new Uri("http://10.0.0.1:8080"), true);
        Assert.Equal(EndpointCategory.InternalIPv4, node3.Category);
    }
    #endregion

    #region 辅助方法
    private static IList<ServiceEndpoint> CreateServices(Int32 count)
    {
        var services = new List<ServiceEndpoint>();
        for (var i = 1; i <= count; i++)
        {
            var svc = new ServiceEndpoint($"service{i}", $"http://127.0.0.1:{10000 + i}")
            {
                Weight = 1
            };
            services.Add(svc);
        }
        return services;
    }
    #endregion
}
