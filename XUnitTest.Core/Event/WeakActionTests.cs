using System.Reflection;
using NewLife;
using Xunit;

namespace XUnitTest.Event;

public class WeakActionTests
{
    #region 基本构造和调用
    [Fact(DisplayName = "实例方法弱引用调用")]
    public void Invoke_InstanceMethod()
    {
        var receiver = new EventReceiver();
        var method = typeof(EventReceiver).GetMethod(nameof(EventReceiver.OnEvent))!;
        var wa = new WeakAction<String>(receiver, (MethodInfo)method);

        wa.Invoke("Hello");

        Assert.Equal("Hello", receiver.LastValue);
    }

    [Fact(DisplayName = "通过委托构造弱引用")]
    public void Invoke_WithDelegate()
    {
        var receiver = new EventReceiver();
        Action<String> handler = receiver.OnEvent;
        var wa = new WeakAction<String>(handler);

        wa.Invoke("World");

        Assert.Equal("World", receiver.LastValue);
    }

    [Fact(DisplayName = "静态方法弱引用调用")]
    public void Invoke_StaticMethod()
    {
        StaticReceiver.LastValue = null;
        Action<Int32> handler = StaticReceiver.OnStaticEvent;
        var wa = new WeakAction<Int32>(handler);

        wa.Invoke(42);

        Assert.Equal(42, StaticReceiver.LastValue);
        Assert.True(wa.IsAlive);
    }

    [Fact(DisplayName = "静态方法target为null")]
    public void Invoke_StaticMethod_NullTarget()
    {
        StaticReceiver.LastValue = null;
        var method = typeof(StaticReceiver).GetMethod(nameof(StaticReceiver.OnStaticEvent))!;
        var wa = new WeakAction<Int32>(null, (MethodInfo)method);

        wa.Invoke(100);

        Assert.Equal(100, StaticReceiver.LastValue);
        Assert.True(wa.IsAlive);
    }
    #endregion

    #region IsAlive
    [Fact(DisplayName = "目标存活时IsAlive为true")]
    public void IsAlive_WhenTargetAlive()
    {
        var receiver = new EventReceiver();
        Action<String> handler = receiver.OnEvent;
        var wa = new WeakAction<String>(handler);

        Assert.True(wa.IsAlive);
    }

    [Fact(DisplayName = "目标被回收后IsAlive为false")]
    public void IsAlive_WhenTargetCollected()
    {
        var wa = CreateWeakAction();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(wa.IsAlive);
    }

    private WeakAction<String> CreateWeakAction()
    {
        var receiver = new EventReceiver();
        Action<String> handler = receiver.OnEvent;
        return new WeakAction<String>(handler);
    }
    #endregion

    #region 一次性事件
    [Fact(DisplayName = "一次性事件调用后自动取消注册")]
    public void Once_UnregistersAfterInvoke()
    {
        var receiver = new EventReceiver();
        Action<String> handler = receiver.OnEvent;
        var unregistered = false;
        Action<Action<String>> unHandler = h => unregistered = true;

        var wa = new WeakAction<String>(handler, unHandler, true);
        wa.Invoke("Once");

        Assert.Equal("Once", receiver.LastValue);
        Assert.True(unregistered);
    }

    [Fact(DisplayName = "非一次性事件不自动取消")]
    public void NotOnce_DoesNotUnregister()
    {
        var receiver = new EventReceiver();
        Action<String> handler = receiver.OnEvent;
        var unregistered = false;
        Action<Action<String>> unHandler = h => unregistered = true;

        var wa = new WeakAction<String>(handler, unHandler, false);
        wa.Invoke("First");
        wa.Invoke("Second");

        Assert.Equal("Second", receiver.LastValue);
        Assert.False(unregistered);
    }
    #endregion

    #region 隐式转换
    [Fact(DisplayName = "WeakAction隐式转换为Action")]
    public void ImplicitConversion()
    {
        var receiver = new EventReceiver();
        Action<String> handler = receiver.OnEvent;
        var wa = new WeakAction<String>(handler);

        Action<String> action = wa;
        action("Converted");

        Assert.Equal("Converted", receiver.LastValue);
    }
    #endregion

    #region ToString
    [Fact(DisplayName = "ToString返回类名和方法名")]
    public void ToStringTest()
    {
        var receiver = new EventReceiver();
        Action<String> handler = receiver.OnEvent;
        var wa = new WeakAction<String>(handler);

        var str = wa.ToString();

        Assert.NotNull(str);
        Assert.Contains("EventReceiver", str!);
        Assert.Contains("OnEvent", str);
    }
    #endregion

    #region 异常场景
    [Fact(DisplayName = "非静态方法target为null抛异常")]
    public void Ctor_NullTarget_NonStaticMethod()
    {
        var method = typeof(EventReceiver).GetMethod(nameof(EventReceiver.OnEvent))!;

        Assert.Throws<InvalidOperationException>(() =>
            new WeakAction<String>(null, (MethodInfo)method));
    }
    #endregion

    #region 辅助类
    class EventReceiver
    {
        public String? LastValue { get; set; }

        public void OnEvent(String value) => LastValue = value;
    }

    static class StaticReceiver
    {
        public static Int32? LastValue { get; set; }

        public static void OnStaticEvent(Int32 value) => LastValue = value;
    }
    #endregion
}
