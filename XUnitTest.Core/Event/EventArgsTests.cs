using NewLife;
using Xunit;

namespace XUnitTest.Event;

public class EventArgsTests
{
    #region 单参数
    [Fact(DisplayName = "单参数EventArgs构造和获取")]
    public void EventArgs_OneArg()
    {
        var e = new EventArgs<String>("Hello");

        Assert.Equal("Hello", e.Arg);
    }

    [Fact(DisplayName = "单参数EventArgs设置属性")]
    public void EventArgs_OneArg_SetArg()
    {
        var e = new EventArgs<Int32>(10);
        e.Arg = 20;

        Assert.Equal(20, e.Arg);
    }

    [Fact(DisplayName = "单参数EventArgs弹出")]
    public void EventArgs_OneArg_Pop()
    {
        var e = new EventArgs<String>("Value1");
        var result = "";
        e.Pop(ref result);

        Assert.Equal("Value1", result);
    }

    [Fact(DisplayName = "单参数EventArgs继承自EventArgs")]
    public void EventArgs_OneArg_InheritsEventArgs()
    {
        var e = new EventArgs<Int32>(42);
        Assert.IsAssignableFrom<EventArgs>(e);
    }
    #endregion

    #region 双参数
    [Fact(DisplayName = "双参数EventArgs构造")]
    public void EventArgs_TwoArgs()
    {
        var e = new EventArgs<String, Int32>("Name", 100);

        Assert.Equal("Name", e.Arg1);
        Assert.Equal(100, e.Arg2);
    }

    [Fact(DisplayName = "双参数EventArgs设置属性")]
    public void EventArgs_TwoArgs_SetArgs()
    {
        var e = new EventArgs<String, Boolean>("test", false);
        e.Arg1 = "updated";
        e.Arg2 = true;

        Assert.Equal("updated", e.Arg1);
        Assert.True(e.Arg2);
    }

    [Fact(DisplayName = "双参数EventArgs弹出")]
    public void EventArgs_TwoArgs_Pop()
    {
        var e = new EventArgs<Int32, String>(42, "Hello");

        var arg1 = 0;
        var arg2 = "";
        e.Pop(ref arg1, ref arg2);

        Assert.Equal(42, arg1);
        Assert.Equal("Hello", arg2);
    }
    #endregion

    #region 三参数
    [Fact(DisplayName = "三参数EventArgs构造")]
    public void EventArgs_ThreeArgs()
    {
        var e = new EventArgs<String, Int32, Boolean>("Name", 100, true);

        Assert.Equal("Name", e.Arg1);
        Assert.Equal(100, e.Arg2);
        Assert.True(e.Arg3);
    }

    [Fact(DisplayName = "三参数EventArgs弹出")]
    public void EventArgs_ThreeArgs_Pop()
    {
        var e = new EventArgs<Int32, String, Double>(1, "two", 3.14);

        var a1 = 0;
        var a2 = "";
        var a3 = 0.0;
        e.Pop(ref a1, ref a2, ref a3);

        Assert.Equal(1, a1);
        Assert.Equal("two", a2);
        Assert.Equal(3.14, a3);
    }
    #endregion

    #region 四参数
    [Fact(DisplayName = "四参数EventArgs构造")]
    public void EventArgs_FourArgs()
    {
        var e = new EventArgs<String, Int32, Boolean, Double>("a", 1, true, 2.5);

        Assert.Equal("a", e.Arg1);
        Assert.Equal(1, e.Arg2);
        Assert.True(e.Arg3);
        Assert.Equal(2.5, e.Arg4);
    }

    [Fact(DisplayName = "四参数EventArgs弹出")]
    public void EventArgs_FourArgs_Pop()
    {
        var e = new EventArgs<Int32, Int32, Int32, Int32>(10, 20, 30, 40);

        var a1 = 0;
        var a2 = 0;
        var a3 = 0;
        var a4 = 0;
        e.Pop(ref a1, ref a2, ref a3, ref a4);

        Assert.Equal(10, a1);
        Assert.Equal(20, a2);
        Assert.Equal(30, a3);
        Assert.Equal(40, a4);
    }

    [Fact(DisplayName = "四参数EventArgs设置属性")]
    public void EventArgs_FourArgs_SetArgs()
    {
        var e = new EventArgs<String, String, String, String>("", "", "", "");
        e.Arg1 = "A";
        e.Arg2 = "B";
        e.Arg3 = "C";
        e.Arg4 = "D";

        Assert.Equal("A", e.Arg1);
        Assert.Equal("B", e.Arg2);
        Assert.Equal("C", e.Arg3);
        Assert.Equal("D", e.Arg4);
    }
    #endregion
}
