using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NewLife;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Configuration;

public class HttpConfigProviderTests
{
    private readonly String _server;
    public HttpConfigProviderTests()
    {
        var file = "config/http.config".GetFullPath();
        if (File.Exists(file))
            _server = File.ReadAllText(file);
        else
        {
            _server = "http://127.0.0.1:7080,http://10.0.0.1:7080";
            File.WriteAllText(file.EnsureDirectory(true), _server);
        }
    }

    [Fact(Skip = "跳过")]
    public void TestApollo()
    {
        var provider = new ApolloConfigProvider
        {
            Server = _server,
            AppId = "testapi"
        };
        provider.SetApollo("application");
        //provider.LoadAll();

        var url = provider["appapiurl"];
        Assert.NotEmpty(url);

        var keys = provider.Keys.ToArray();
        Assert.NotNull(keys);

        var model = provider.Load<Model>();
        Assert.NotNull(model);
        Assert.NotEmpty(model.AppApiUrl);
        Assert.Equal(url, model.AppApiUrl);
        Assert.True(model.Radius > 0);
        Assert.NotEmpty(model.MySqlServer);

        var model2 = new Model();
        provider.Bind(model2);
        Assert.Equal(url, model2.AppApiUrl);
        Assert.True(model2.Radius > 0);
        Assert.NotEmpty(model2.MySqlServer);
    }

    private class Model
    {
        public Int32 Radius { get; set; }

        public String MySqlServer { get; set; }

        public String AppApiUrl { get; set; }
    }

    [Fact(Skip = "跳过")]
    public void TestStardust()
    {
        var set = new ConfigModel
        {
            Debug = true,
            LogLevel = LogLevel.Fatal,
            LogPath = "xxx",
            NetworkLog = "255.255.255.255:514",
            TempPath = "yyy",

            Sys = new SysConfig
            {
                Name = "NewLife.Cube",
                DisplayName = "魔方平台",
                Company = "新生命开发团队",
            },
        };

        var provider = new HttpConfigProvider
        {
            Server = "http://star.newlifex.com:6600",
            //Server = "http://localhost:6600",
            AppId = "Test"
        };

        provider.Save(set);

        provider.Save(new Model2 { Test = "测试", Shop = "商店", Title = "NewLife开发团队" });


        var str = provider["test1"];
        Assert.NotEmpty(str);

        var keys = provider.Keys.ToArray();
        Assert.NotNull(keys);

        var model = provider.Load<Model2>();
        Assert.NotNull(model);
        Assert.NotEmpty(model.Test);
        Assert.Equal(str, model.Test);
        Assert.NotEmpty(model.Shop);
        Assert.NotEmpty(model.Title);
        Assert.Equal("NewLife开发团队", model.Title);

        var model2 = new Model2();
        provider.Bind(model2);
        Assert.Equal(str, model2.Test);
        Assert.NotEmpty(model.Shop);
        Assert.Equal("NewLife开发团队", model.Title);

        provider.LoadAll();
    }

    [Fact(Skip = "跳过")]
    public void TestHttpConfigAttribute()
    {
        var c = HttpConfigModel.Current;
        Assert.Equal("1234", c.Test);
        Assert.Equal("NewLife开发团队", c.Title);
    }

    private class Model2
    {
        [DataMember(Name = "test1")]
        public String Test { get; set; }

        [DataMember(Name = "conn_Shop")]
        public String Shop { get; set; }

        public String Title { get; set; }
    }

    [Fact]
    public void TestLayers()
    {
        var dic = new Dictionary<String, Object>
        {
            ["name"] = "stone",
            ["cls:server"] = "http://127.0.0.1",
            ["cls:topic"] = "mytopic"
        };

        var prv = new HttpConfigProvider();
        var rs = prv.Build(dic);

        Assert.Equal(2, rs.Childs.Count);
        Assert.Equal("name", rs.Childs[0].Key);
        Assert.Equal("stone", rs.Childs[0].Value);

        var section = rs.Childs[1];
        Assert.Equal("cls", section.Key);
        Assert.Null(section.Value);
        Assert.Equal(2, section.Childs.Count);
        Assert.Equal("server", section.Childs[0].Key);
        Assert.Equal("http://127.0.0.1", section.Childs[0].Value);
        Assert.Equal("topic", section.Childs[1].Key);
        Assert.Equal("mytopic", section.Childs[1].Value);

        prv.Root = rs;

        var cls = prv.Load<MyCls>("cls");
        Assert.NotNull(cls);
        Assert.Equal("http://127.0.0.1", cls.Server);
        Assert.Equal("mytopic", cls.Topic);

        Assert.Equal("http://127.0.0.1", prv["cls:Server"]);
        Assert.Equal("mytopic", prv["cls:Topic"]);
    }

    [Fact(Skip = "跳过")]
    public void TestStardustLayers()
    {
        var prv = new HttpConfigProvider
        {
            Server = "http://star.newlifex.com:6600",
            //Server = "http://localhost:6600",
            AppId = "test"
        };

        var cls = prv.Load<MyCls>("cls");
        Assert.NotNull(cls);
        Assert.Equal("http://127.0.0.1", cls.Server);
        Assert.Equal("mytopic", cls.Topic);

        Assert.Equal("http://127.0.0.1", prv["cls:Server"]);
        Assert.Equal("mytopic", prv["cls:Topic"]);
    }

    class MyCls
    {
        public String Server { get; set; }

        public String Topic { get; set; }
    }
}