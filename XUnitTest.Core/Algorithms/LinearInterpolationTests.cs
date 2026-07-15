using System.ComponentModel;
using NewLife.Algorithms;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Algorithms;

/// <summary>线性插值测试</summary>
public class LinearInterpolationTests
{
    private readonly LinearInterpolation _target = new();

    private static TimePoint[] CreatePoints(params (Int64 time, Double value)[] items)
    {
        return items.Select(item => new TimePoint { Time = item.time, Value = item.value }).ToArray();
    }

    [Fact]
    [DisplayName("Process_区间内插值_正确计算")]
    public void Process_InterpolateBetween_ReturnsInterpolatedValue()
    {
        // (t0=0, v0=0), (t1=10, v1=10), current=5 → v=5
        var data = CreatePoints((0, 0), (10, 10));
        var result = _target.Process(data, 0, 1, 5);
        Assert.Equal(5, result);
    }

    [Fact]
    [DisplayName("Process_区间内插值_非对称值")]
    public void Process_InterpolateBetween_NonSymmetric()
    {
        // (t0=0, v0=10), (t1=100, v1=20), current=50 → v=15
        var data = CreatePoints((0, 10), (100, 20));
        var result = _target.Process(data, 0, 1, 50);
        Assert.Equal(15, result);
    }

    [Fact]
    [DisplayName("Process_当前时间等于起点_返回起点值")]
    public void Process_CurrentEqualsPrev_ReturnsPrevValue()
    {
        var data = CreatePoints((10, 100), (20, 200));
        var result = _target.Process(data, 0, 1, 10);
        Assert.Equal(100, result);
    }

    [Fact]
    [DisplayName("Process_当前时间等于终点_返回终点值")]
    public void Process_CurrentEqualsNext_ReturnsNextValue()
    {
        var data = CreatePoints((10, 100), (20, 200));
        var result = _target.Process(data, 0, 1, 20);
        Assert.Equal(200, result);
    }

    [Fact]
    [DisplayName("Process_左外推_current小于起点_外推计算")]
    public void Process_ExtrapolateLeft_ReturnsExtrapolatedValue()
    {
        // (t0=10, v0=20), (t1=20, v1=40), current=0 → v=0
        // rate = (40-20)/(20-10) = 2
        // v = 20 + (0-10)*2 = 0
        var data = CreatePoints((10, 20), (20, 40));
        var result = _target.Process(data, 0, 1, 0);
        Assert.Equal(0, result);
    }

    [Fact]
    [DisplayName("Process_右外推_current大于终点_外推计算")]
    public void Process_ExtrapolateRight_ReturnsExtrapolatedValue()
    {
        // (t0=10, v0=20), (t1=20, v1=40), current=30 → v=60
        // rate = (40-20)/(20-10) = 2
        // v = 20 + (30-10)*2 = 60
        var data = CreatePoints((10, 20), (20, 40));
        var result = _target.Process(data, 0, 1, 30);
        Assert.Equal(60, result);
    }

    [Fact]
    [DisplayName("Process_prev等于next_返回该点值")]
    public void Process_PrevEqualsNext_ReturnsValue()
    {
        var data = CreatePoints((5, 42));
        var result = _target.Process(data, 0, 0, 100);
        Assert.Equal(42, result);
    }

    [Fact]
    [DisplayName("Process_时间差为零_返回起点值避免除零")]
    public void Process_ZeroTimeDifference_ReturnsPrevValue()
    {
        // prev 和 next 时间相同但不同索引，不应除零
        var data = CreatePoints((10, 100), (10, 999));
        var result = _target.Process(data, 0, 1, 15);
        Assert.Equal(100, result);
    }

    [Fact]
    [DisplayName("Process_Null数据_抛出ArgumentNullException")]
    public void Process_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _target.Process(null!, 0, 1, 10));
    }

    [Fact]
    [DisplayName("Process_空数组_抛出ArgumentNullException")]
    public void Process_EmptyArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _target.Process([], 0, 1, 10));
    }

    [Fact]
    [DisplayName("Process_prev越界_抛出ArgumentOutOfRangeException")]
    public void Process_PrevOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var data = CreatePoints((0, 0), (10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => _target.Process(data, -1, 1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => _target.Process(data, 5, 1, 5));
    }

    [Fact]
    [DisplayName("Process_next越界_抛出ArgumentOutOfRangeException")]
    public void Process_NextOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var data = CreatePoints((0, 0), (10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => _target.Process(data, 0, -1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => _target.Process(data, 0, 5, 5));
    }

    [Fact]
    [DisplayName("Process_单元素数组_prev=next=0_返回该值")]
    public void Process_SingleElement_ReturnsValue()
    {
        var data = CreatePoints((100, 3.14));
        var result = _target.Process(data, 0, 0, 999);
        Assert.Equal(3.14, result);
    }

    [Theory]
    [DisplayName("Process_多组插值_使用InlineData")]
    [InlineData(0, 10, 0, 10, 5, 5.0)]
    [InlineData(0, 10, 10, 20, 5, 15.0)]
    [InlineData(100, 200, 1, 10, 150, 5.5)]
    public void Process_MultipleCases_CorrectResults(
        Int64 t0, Int64 t1, Double v0, Double v1, Int64 current, Double expected)
    {
        var data = CreatePoints((t0, v0), (t1, v1));
        var result = _target.Process(data, 0, 1, current);
        Assert.Equal(expected, result, precision: 6);
    }
}
