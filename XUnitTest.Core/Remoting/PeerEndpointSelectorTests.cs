using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Remoting;

public class PeerEndpointSelectorTests
{
    [Fact(DisplayName = "地址排序_内网优先")]
    public void Order_InternalFirst()
    {
        var selector = new PeerEndpointSelector();
        selector.SetAddresses("http://192.168.1.10:6680,http://[fd00::1]:6680", "http://1.2.3.4:6680,http://star.newlifex.com,http://[2408::1]:6680");

        var result = selector.GetOrderedEndpoints();

        Assert.Equal("http://192.168.1.10:6680/", result[0].Address.AbsoluteUri);
        Assert.Equal("http://[fd00::1]:6680/", result[1].Address.AbsoluteUri);
        Assert.Equal("http://1.2.3.4:6680/", result[2].Address.AbsoluteUri);
        Assert.Equal("http://star.newlifex.com/", result[3].Address.AbsoluteUri);
        Assert.Equal("http://[2408::1]:6680/", result[4].Address.AbsoluteUri);
    }

    [Fact(DisplayName = "失败屏蔽_可用地址优先")]
    public void Failure_ShieldsEndpoint()
    {
        var selector = new PeerEndpointSelector { ShieldingSeconds = 600 };
        selector.SetAddresses("http://10.0.0.2:6680", "http://8.8.8.8:6680");

        selector.MarkFailure("http://10.0.0.2:6680", null);

        var ordered = selector.GetOrderedEndpoints();

        Assert.Single(ordered);
        Assert.Equal("http://8.8.8.8:6680/", ordered[0].Address.AbsoluteUri);
    }

    [Fact(DisplayName = "成功更新RTT排序")]
    public void Success_UpdatesRttOrder()
    {
        var selector = new PeerEndpointSelector();
        selector.SetAddresses("http://10.0.0.2:6680,http://10.0.0.3:6680", null);

        selector.MarkSuccess("http://10.0.0.2:6680", TimeSpan.FromMilliseconds(200));
        selector.MarkSuccess("http://10.0.0.3:6680", TimeSpan.FromMilliseconds(50));

        var ordered = selector.GetOrderedEndpoints();

        Assert.Equal("http://10.0.0.3:6680/", ordered[0].Address.AbsoluteUri);
        Assert.Equal("http://10.0.0.2:6680/", ordered[1].Address.AbsoluteUri);
    }

    [Fact(DisplayName = "探测委托_更新可用性")]
    public async Task ProbeDelegate_UpdatesState()
    {
        var selector = new PeerEndpointSelector
        {
            ProbeTimeout = 200,
            ShieldingSeconds = 60,
            ProbeAsync = (uri, _) =>
            {
                if (uri.Host.StartsWith("ok", StringComparison.OrdinalIgnoreCase)) return Task.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(30));
                return Task.FromResult<TimeSpan?>(null);
            }
        };

        selector.SetAddresses("http://ok.internal:6680", "http://fail.external:6680");

        var ordered = await selector.GetOrderedEndpointsAsync(true, CancellationToken.None);

        Assert.Single(ordered);
        Assert.Equal("ok.internal", ordered[0].Address.Host);
        Assert.True(ordered[0].IsUp);

        ordered = selector.Endpoints.AsReadOnly();
        Assert.False(ordered[1].IsUp);
        Assert.True(ordered[1].NextProbe > DateTime.Now);
    }

    [Fact(DisplayName = "无可用地址_返回空集合")]
    public void NoUsable_ReturnAllWithScore()
    {
        var selector = new PeerEndpointSelector();
        selector.SetAddresses("http://10.0.0.2:6680,http://10.0.0.3:6680", null);

        selector.MarkFailure("http://10.0.0.2:6680", null);
        selector.MarkFailure("http://10.0.0.3:6680", null);

        var ordered = selector.GetOrderedEndpoints();

        Assert.Empty(ordered);
        //Assert.Equal(2, ordered.Count);
        //Assert.All(ordered, e => Assert.InRange(e.Score, 0, 1000));
        //Assert.Equal(0, ordered[0].Score);
        //Assert.True(ordered[1].Score >= 100);
    }

    [Fact(DisplayName = "分数被限制在范围内")]
    public void Score_IsClamped()
    {
        var selector = new PeerEndpointSelector();
        selector.SetAddresses("http://10.0.0.2:6680", null);

        var state = selector.Endpoints[0];
        state.Score = 500;

        var ordered = selector.GetOrderedEndpoints();

        Assert.InRange(ordered[0].Score, 0, 1000);
    }

    //[Fact(DisplayName = "分数受RTT影响")]
    //public void Score_RespectsRtt()
    //{
    //    var selector = new PeerEndpointSelector();
    //    selector.SetAddresses("http://10.0.0.2:6680", null);

    //    selector.MarkSuccess("http://10.0.0.2:6680", TimeSpan.FromMilliseconds(500));

    //    var ordered = selector.GetOrderedEndpoints();

    //    Assert.True(ordered[0].Score >= 100);
    //}
}
