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

    [Fact]
    public void KeyNotFound4()
    {
        var ext = new HandlerContext();
        Assert.Null(ext["bbb"]);
        //var ex = Assert.Throws<KeyNotFoundException>(() => ext["bbb"]);
    }
}