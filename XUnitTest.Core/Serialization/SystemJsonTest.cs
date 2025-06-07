﻿using System.Text.Json.Serialization;
using NewLife;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class SystemJsonTest : JsonTestBase
{
    SystemJson _json = new SystemJson();

    [Fact(DisplayName = "基础测试")]
    public void Test1()
    {
        var set = new Setting
        {
            LogLevel = LogLevel.Error,
            LogPath = "xxx",
        };

        var js = _json.Write(set, true, false, false);
        Assert.True(js.StartsWith("{") && js.EndsWith("}"));

        var set2 = _json.Read(js, typeof(Setting)) as Setting;

        Assert.Equal(LogLevel.Error, set2.LogLevel);
        Assert.Equal("xxx", set2.LogPath);
    }

    [Fact]
    public void DateTimeTest()
    {
        var model = new Model();
        Rand.Fill(model);
        model.Roles = ["admin", "user"];
        model.Scores = [1, 2, 3];
        var js = _json.Write(model, true);

        var models = _json.Read(_json_value, typeof(Model[])) as Model[];
        Assert.Equal(2, models.Length);

        CheckModel(models);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public void InterfaceTest()
    {
        var list = new List<IDuck>
        {
            new DuckB { Name = "123" },
            new DuckB { Name = "456" },
            new DuckB { Name = "789" }
        };
        var model = new ModelA
        {
            ID = 2233,
            Childs = list,
        };

        var js = new SystemJson();
        //var json = model.ToJson();
        var json = js.Write(model);

        //// 直接反序列化会抛出异常
        //Assert.Throws<Exception>(() => js.Read(json, typeof(ModelA)));

        //// 上对象容器
        //ObjectContainer.Current.AddTransient<IDuck, DuckB>();

        // 再来一次反序列化
        //var model2 = json.ToJsonEntity<ModelA>();
        var model2 = js.Read(json, typeof(ModelA)) as ModelA;
        Assert.NotNull(model2);
        Assert.Equal(2233, model2.ID);
        Assert.Equal(3, model2.Childs.Count);
        Assert.Equal("123", model.Childs[0].Name);
        Assert.Equal("456", model.Childs[1].Name);
        Assert.Equal("789", model.Childs[2].Name);
    }

    [Fact]
    public void InterfaceTest2()
    {
        var list = new List<IDuck>
        {
            new DuckB { Name = "123" },
            new DuckB { Name = "456" },
            new DuckB { Name = "789" }
        };
        var model = new ModelA
        {
            ID = 2233,
            Childs = list,
        };

        var js = new SystemJson();
        var services = new ObjectContainer();
        js.ServiceProvider = services.BuildServiceProvider();

        var json = model.ToJson();
        //var json = js.Write(model);

        //// 直接反序列化会抛出异常
        //Assert.Throws<NotSupportedException>(() => js.Read(json, typeof(ModelA)));

        // 上对象容器。必须在使用前注册好服务
        services.AddTransient<IDuck, DuckB>();

        // 再来一次反序列化
        //var model2 = json.ToJsonEntity<ModelA>();
        var model2 = js.Read(json, typeof(ModelA)) as ModelA;
        Assert.NotNull(model2);
        Assert.Equal(2233, model2.ID);
        Assert.Equal(3, model2.Childs.Count);
        Assert.Equal("123", model.Childs[0].Name);
        Assert.Equal("456", model.Childs[1].Name);
        Assert.Equal("789", model.Childs[2].Name);
    }

    [JsonDerivedType(typeof(DuckB), typeDiscriminator: "duckb")]
    interface IDuck
    {
        public String Name { get; set; }
    }

    class ModelA
    {
        public Int32 ID { get; set; }

        public IList<IDuck> Childs { get; set; }

        public Object Body { get; set; }
    }

    class DuckB : IDuck
    {
        public String Name { get; set; }
    }

    [Fact]
    public void ObjectTest()
    {
        var model = new ModelB
        {
            ID = 2233,
            Body = new
            {
                aaa = 123,
                bbb = 456,
                ccc = 789,
            },
        };

        // 序列化
        //var json = model.ToJson();
        var json = _json.Write(model);

        // 反序列化
        //var model2 = json.ToJsonEntity<ModelB>();
        var model2 = _json.Read(json, typeof(ModelA)) as ModelA;
        Assert.NotNull(model2);
        Assert.Equal(2233, model2.ID);

        var dic = model2.Body.ToDictionary();
        Assert.Equal(3, dic.Count);
        Assert.Equal(123, dic["aaa"]);
        Assert.Equal(456, dic["bbb"]);
        Assert.Equal(789, dic["ccc"]);
    }

    class ModelB
    {
        public Int32 ID { get; set; }

        public Object Body { get; set; }
    }

    [Fact]
    public void Decode()
    {
        var model = new ModelB
        {
            ID = 2233,
            Body = new
            {
                aaa = 123,
                bbb = 456,
                ccc = 789,
            },
        };

        // 序列化
        var json = _json.Write(model);

        var dic = _json.Decode(json);
        Assert.NotNull(dic);

        Assert.Equal(2233, dic["id"]);

        dic = dic["body"] as IDictionary<String, Object>;
        Assert.NotNull(dic);
        Assert.Equal(3, dic.Count);
        Assert.Equal(123, dic["aaa"]);
        Assert.Equal(456, dic["bbb"]);
        Assert.Equal(789, dic["ccc"]);
    }

    [Fact]
    public void ReadTime()
    {
        var json = """{"Code":null,"Secret":null,"ClientId":"10.0.0.12@2116","Version":"1.0.2025.0523","Compile":1747977022000,"IP":"10.0.0.12","Macs":"90-FC-42-B5-09-C8,A4-15-C1-79-14-45","UUID":"C40E8D38C77A2BD79419B8C424A0F36E","Time":1747987231793,"ProductCode":"A2FullTest","Name":"A2","OSName":"Ubuntu 18.04 LTS","OSVersion":"3.4.39.0","Architecture":"Arm","MachineName":"A2","UserName":"root","ProcessorCount":4,"Memory":521736192,"TotalSize":7637516288}""";

        XTrace.WriteLine(json);

        var js = new SystemJson();
        var info = js.Read<NodeInfo>(json);
        XTrace.WriteLine(info.Compile.ToFullString());
        Assert.Equal("2025-05-23 13:10:22".ToDateTime(), info.Compile);
    }

    class NodeInfo
    {
        public DateTime Compile { get; set; }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnumTest(Boolean enumString)
    {
        // 字符串
        var sjs = new SystemJson();
        var option = new JsonOptions { EnumString = enumString };

        var data = new { Level = LogLevel.Fatal };
        var json = sjs.Write(data, option);

        var js = new JsonParser(json);
        var dic = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(dic);

        if (enumString)
            Assert.Equal("Fatal", dic["Level"]);
        else
        {
            Assert.Equal(5, dic["Level"]);
            Assert.Equal((Int32)LogLevel.Fatal, dic["Level"].ToInt());
        }
    }
#endif
}
