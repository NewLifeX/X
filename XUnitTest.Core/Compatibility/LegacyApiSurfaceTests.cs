using System.Reflection;
using NewLife.Buffers;
using NewLife.Collections;
using NewLife.Data;
using Xunit;

#pragma warning disable CS0618 // 兼容面测试需要引用标记为过时的类型

namespace XUnitTest.Compatibility;

/// <summary>v11 二进制兼容面回归测试</summary>
/// <remarks>
/// <para>用于锁定"为兼容旧二进制而保留的过时成员"。这些成员删除后，基于 v11 编译的下游程序集会在运行时抛
/// <c>MissingMethodException</c>/<c>TypeLoadException</c>，因此必须保持存在且签名与 v11 一致。</para>
/// <para>全部通过反射按名称查找，避免测试代码自身触发过时成员的编译期诊断。</para>
/// </remarks>
public class LegacyApiSurfaceTests
{
    private static readonly Type[] _slice3Pars = [typeof(Int32), typeof(Int32), typeof(Boolean)];

    [Fact(DisplayName = "兼容面：三参 Slice 在接口与 OwnerPacket 上保留，返回类型与 v11 一致")]
    public void SliceThreeParams()
    {
        var m = typeof(IPacket).GetMethod("Slice", _slice3Pars);
        Assert.NotNull(m);
        Assert.Equal(typeof(IPacket), m!.ReturnType);

        Assert.Equal(typeof(IPacket), typeof(OwnerPacket).GetMethod("Slice", _slice3Pars)!.ReturnType);
    }

    [Fact(DisplayName = "单参构造保留且不过时；双参构造不设默认值，避免单参调用吃默认值")]
    public void OwnerPacketConstructors()
    {
        // v11 编译的调用方依赖 (Int32) 签名，必须保留
        var one = typeof(OwnerPacket).GetConstructor([typeof(Int32)]);
        Assert.NotNull(one);

        // 单参构造是一等重载（"不预留"），不应标记过时：否则 new OwnerPacket(n) 永远吃到过时警告
        Assert.Null(one!.GetCustomAttribute<ObsoleteAttribute>());

        // 双参构造不设默认值：单参调用总是绑定到上面的重载，默认值对源码调用永久失效
        var two = typeof(OwnerPacket).GetConstructor([typeof(Int32), typeof(Int32)]);
        Assert.NotNull(two);
        Assert.False(two!.GetParameters()[1].HasDefaultValue);
    }

    [Fact(DisplayName = "兼容面：三参 Slice 与两参等价——共享引用计数，源句柄仍可用")]
    public void SliceThreeParamsBehavesAsTwo()
    {
        using var pk = new OwnerPacket(8);
        pk.GetSpan().Fill(7);

        var method = typeof(OwnerPacket).GetMethod("Slice", _slice3Pars)!;
        var sub = (IDisposable)method.Invoke(pk, [2, 3, false])!;
        try
        {
            Assert.Equal(3, ((IPacket)sub).Length);
            Assert.Equal(8, pk.Length);     // 源句柄不受影响
            Assert.Equal(2, pk.RefCount);   // 引用计数共享
        }
        finally
        {
            sub.Dispose();
        }

        Assert.Equal(1, pk.RefCount);
    }

    [Fact(DisplayName = "兼容面：其余 v11 成员按名与签名保留")]
    public void OtherLegacyMembers()
    {
        // 基础/工具
        Assert.NotNull(typeof(NewLife.Reflection.Reflect).GetMethod("GetTypeEx", [typeof(String), typeof(Boolean)]));
        Assert.NotNull(typeof(SpanReader).GetProperty("FreeCapacity"));
        Assert.NotNull(typeof(ObjectPool<>).MakeGenericType(typeof(String)).GetProperty("AllIdleTime"));
        Assert.NotNull(typeof(NewLife.Model.ObjectContainerHelper).GetMethod("Resolve", 1, [typeof(NewLife.Model.IObjectContainer)]));

        // 网络
        Assert.NotNull(typeof(NewLife.NetHelper).GetMethod("GetAllTcpConnections", Type.EmptyTypes));
    }

    [Fact(DisplayName = "兼容面：AllIdleTime 与 IdleTime 是同一设置项的读写代理")]
    public void AllIdleTimeProxiesIdleTime()
    {
        var pool = new ObjectPool<String>();
        var prop = pool.GetType().GetProperty("AllIdleTime")!;

        prop.SetValue(pool, 123);
        Assert.Equal(123, pool.IdleTime);
        Assert.Equal(123, prop.GetValue(pool));

        pool.IdleTime = 45;
        Assert.Equal(45, prop.GetValue(pool));

        pool.Dispose();
    }
}
