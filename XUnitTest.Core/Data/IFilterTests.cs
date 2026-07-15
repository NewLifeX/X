using System.ComponentModel;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class IFilterTests
{
    private class AddHeaderFilter : FilterBase
    {
        public Byte HeaderValue { get; set; }

        protected override Boolean OnExecute(FilterContext context)
        {
            if (context.Packet is ArrayPacket ap)
            {
                var data = ap.ToArray();
                var newData = new Byte[data.Length + 1];
                newData[0] = HeaderValue;
                data.CopyTo(newData, 1);
                context.Packet = new ArrayPacket(newData);
            }
            return true;
        }
    }

    private class TerminateFilter : FilterBase
    {
        protected override Boolean OnExecute(FilterContext context)
        {
            // 返回 false 终止责任链
            return false;
        }
    }

    private class DropFilter : FilterBase
    {
        protected override Boolean OnExecute(FilterContext context)
        {
            // 设置 Packet 为 null 终止责任链
            context.Packet = null;
            return true;
        }
    }

    [Fact]
    [DisplayName("Execute_单过滤器_正常处理")]
    public void Execute_SingleFilter_Processes()
    {
        var filter = new AddHeaderFilter { HeaderValue = 0xAA };
        var packet = new ArrayPacket([0x01, 0x02, 0x03]);
        var ctx = new FilterContext { Packet = packet };

        filter.Execute(ctx);

        Assert.NotNull(ctx.Packet);
        var data = ctx.Packet.ToArray();
        Assert.Equal(4, data.Length);
        Assert.Equal(0xAA, data[0]);
        Assert.Equal([0xAA, 0x01, 0x02, 0x03], data);
    }

    [Fact]
    [DisplayName("Execute_过滤器链_顺序执行")]
    public void Execute_FilterChain_ExecutesInOrder()
    {
        var filter1 = new AddHeaderFilter { HeaderValue = 0xAA };
        var filter2 = new AddHeaderFilter { HeaderValue = 0xBB };
        filter1.Next = filter2;

        var packet = new ArrayPacket([0x01]);
        var ctx = new FilterContext { Packet = packet };

        filter1.Execute(ctx);

        Assert.NotNull(ctx.Packet);
        var data = ctx.Packet.ToArray();
        // filter1 在前面添加 0xAA → [0xAA, 0x01]
        // filter2 在前面添加 0xBB → [0xBB, 0xAA, 0x01]
        Assert.Equal(3, data.Length);
        Assert.Equal(0xBB, data[0]);
        Assert.Equal(0xAA, data[1]);
        Assert.Equal(0x01, data[2]);
    }

    [Fact]
    [DisplayName("OnExecute返回False_终止责任链")]
    public void OnExecuteReturnsFalse_TerminatesChain()
    {
        var terminator = new TerminateFilter();
        var nextFilter = new AddHeaderFilter { HeaderValue = 0xCC };
        terminator.Next = nextFilter;

        var packet = new ArrayPacket([0x01]);
        var ctx = new FilterContext { Packet = packet };

        terminator.Execute(ctx);

        // 后续过滤器不应被执行，数据不变
        Assert.NotNull(ctx.Packet);
        var data = ctx.Packet.ToArray();
        Assert.Equal([0x01], data);
    }

    [Fact]
    [DisplayName("Packet为null_终止责任链")]
    public void PacketIsNull_TerminatesChain()
    {
        var dropper = new DropFilter();
        var nextFilter = new AddHeaderFilter { HeaderValue = 0xDD };
        dropper.Next = nextFilter;

        var packet = new ArrayPacket([0x01]);
        var ctx = new FilterContext { Packet = packet };

        dropper.Execute(ctx);

        // Packet 被设置为 null，后续过滤器不应执行
        Assert.Null(ctx.Packet);
    }

    [Fact]
    [DisplayName("Find_查找链中指定类型的过滤器")]
    public void Find_FindsFilterInChain()
    {
        var filter1 = new AddHeaderFilter { HeaderValue = 0xAA };
        var filter2 = new AddHeaderFilter { HeaderValue = 0xBB };
        filter1.Next = filter2;

        var found = filter1.Find<AddHeaderFilter>();
        Assert.NotNull(found);
        Assert.Equal(0xAA, found.HeaderValue);

        // 查找第二个
        var found2 = filter1.Find(typeof(AddHeaderFilter));
        Assert.NotNull(found2);
    }

    [Fact]
    [DisplayName("Find_不存在的类型_返回null")]
    public void Find_NonExistingType_ReturnsNull()
    {
        var filter = new AddHeaderFilter { HeaderValue = 0xAA };

        var found = filter.Find<TerminateFilter>();
        Assert.Null(found);
    }

    [Fact]
    [DisplayName("Find_null输入_返回null")]
    public void Find_NullInput_ReturnsNull()
    {
        IFilter? nullFilter = null;
        var found = nullFilter.Find(typeof(AddHeaderFilter));
        Assert.Null(found);
    }
}
