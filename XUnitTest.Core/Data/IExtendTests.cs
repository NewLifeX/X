using System;
using System.Collections.Generic;
using System.Net;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Model;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Data;

public class IExtendTests
{
    class ExtendTest : IExtend
    {
        public IDictionary<String, Object> Items { get; } = new Dictionary<String, Object>();

        public Object this[String key] { get => Items[key]; set => Items[key] = value; }
    }

    [Fact]
    public void ToDictionary_Interface()
    {
        var ext = new ExtendTest();
        ext["aaa"] = 1234;

        var dic = ext.ToDictionary();
        Assert.NotNull(dic);
        //Assert.Equal(typeof(ExtendTest), dic.GetType());
        Assert.Equal(1234, dic["aaa"]);

        dic["bbb"] = "xxx";
        //Assert.Equal("xxx", ext["bbb"]);
        var ex = Assert.Throws<KeyNotFoundException>(() => ext["bbb"]);
    }

    [Fact]
    public void ToDictionary_RefrectItems()
    {
        var ext = new ExtendTest3
        {
            ["aaa"] = 1234
        };

        var dic = ext.ToDictionary();
        Assert.NotNull(dic);
        Assert.Equal(typeof(NullableDictionary<String, Object>), dic.GetType());
        Assert.Equal(1234, dic["aaa"]);

        // 引用型
        dic["bbb"] = "xxx";
        Assert.Null(ext["bbb"]);
        //var ex = Assert.Throws<KeyNotFoundException>(() => ext["bbb"]);
    }

    class ExtendTest3 : IExtend
    {
        public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>();

        public Object this[String item] { get => Items[item]; set => Items[item] = value; }
    }

    [Fact]
    public void KeyNotFound1()
    {
        var ss = new TcpSession();
        var ext = new NetSession { Session = ss };
        Assert.Null(ext["bbb"]);
        //var ex = Assert.Throws<KeyNotFoundException>(() => ext["bbb"]);

        var ext2 = new NetServer();
        Assert.Null(ext2["bbb"]);
    }

    [Fact]
    public void KeyNotFound2()
    {
        var ext = new TcpSession();
        Assert.Null(ext["bbb"]);
        //var ex = Assert.Throws<KeyNotFoundException>(() => ext["bbb"]);
    }

    [Fact]
    public void KeyNotFound3()
    {
        var ext = new UdpSession(new UdpServer(), null, new IPEndPoint(IPAddress.Loopback, 0));
        Assert.Null(ext["bbb"]);
        //var ex = Assert.Throws<KeyNotFoundException>(() => ext["bbb"]);
    }

    [Fact(DisplayName = "扩展数据_并发首访_不丢弃任何写入")]
    public void Items_ConcurrentFirstAccess_NoLostWrite()
    {
        // 懒创建字典：并发首访若各自新建，败者那份里刚写入的数据会随之不可达而丢失
        using (var server = new NetServer()) AssertNoLostWrite(() => server.Items);
        using (var client = new NetClient()) AssertNoLostWrite(() => client.Items);
        using (var us = new UdpServer())
        using (var session = new UdpSession(us, null, new IPEndPoint(IPAddress.Loopback, 0))) AssertNoLostWrite(() => session.Items);
    }

    /// <summary>并发首访扩展数据字典：每个线程写入的键都必须保留</summary>
    /// <remarks>竞态非必现，用 Barrier 制造“同时首访”提高命中率；修复后恒定通过，旧实现高概率变红</remarks>
    private static void AssertNoLostWrite(Func<IDictionary<String, Object?>> getItems)
    {
        const Int32 count = 32;

        using var barrier = new Barrier(count);
        var threads = new Thread[count];
        for (var i = 0; i < count; i++)
        {
            var k = i;
            threads[i] = new Thread(() =>
            {
                barrier.SignalAndWait();
                getItems()["k" + k] = k;
            })
            { IsBackground = true };
        }

        foreach (var th in threads) th.Start();
        foreach (var th in threads) Assert.True(th.Join(10_000), "并发写入未在超时内完成");

        Assert.Equal(count, getItems().Count);
    }
}