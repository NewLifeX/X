using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NewLife;
using NewLife.Log;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class SystemJsonTest
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
        var str = """
            [
                {
                    "ID": 0,
                    "Userid": 27,
                    "ClickTime": "2020-03-09T21:16:17.88",
                    "AdID": 39,
                    "AdAmount": 0.43,
                    "isGive": false,
                    "AdLinkUrl": "http://www.baidu.com",
                    "AdImgUrl": "/uploader/swiperPic/405621836.jpg",
                    "Type": "NewLife.Common.PinYin",
                    "Offset": "2022-11-29T14:13:17.8763881+08:00"
                },
                {
                    "ID": 0,
                    "Userid": 27,
                    "ClickTime": "2020-03-09T21:16:25.9052764+08:00",
                    "AdID": 40,
                    "AdAmount": 0.41,
                    "isGive": false,
                    "AdLinkUrl": "http://www.baidu.com",
                    "AdImgUrl": "/uploader/swiperPic/1978468752.jpg",
                    "Type": "String",
                    "Offset": "2022-11-29T14:13:17.8763881+08:00"
                }
            ]
            """;

        var models = _json.Read(str, typeof(Model[])) as Model[];
        Assert.Equal(2, models.Length);

        var m = models[0];
        Assert.Equal(27, m.UserId);
        Assert.Equal(new DateTime(2020, 3, 9, 21, 16, 17, 880).ToUniversalTime(), m.ClickTime.ToUniversalTime());
        Assert.Equal(39, m.AdId);
        Assert.Equal(0.43, m.AdAmount);
        Assert.False(m.IsGive);
        Assert.Equal("http://www.baidu.com", m.AdLinkUrl);
        Assert.Equal("/uploader/swiperPic/405621836.jpg", m.AdImgUrl);
        Assert.Equal(typeof(NewLife.Common.PinYin), m.Type);
        Assert.Equal(DateTimeOffset.Parse("2022-11-29T14:13:17.8763881+08:00"), m.Offset);

        m = models[1];
        Assert.Equal(27, m.UserId);
        Assert.Equal("2020-03-09T21:16:25.9052764+08:00".ToDateTimeOffset().Trim("us").ToUniversalTime(), m.ClickTime.Trim("us").ToUniversalTime());
        Assert.Equal(40, m.AdId);
        Assert.Equal(0.41, m.AdAmount);
        Assert.False(m.IsGive);
        Assert.Equal("http://www.baidu.com", m.AdLinkUrl);
        Assert.Equal("/uploader/swiperPic/1978468752.jpg", m.AdImgUrl);
        Assert.Equal(typeof(String), m.Type);
        Assert.Equal(DateTimeOffset.Parse("2022-11-29T14:13:17.8763881+08:00"), m.Offset);
    }

    class Model
    {
        public Int32 ID { get; set; }
        public Int32 UserId { get; set; }
        public DateTime ClickTime { get; set; }
        public Int32 AdId { get; set; }
        public Double AdAmount { get; set; }
        public Boolean IsGive { get; set; }
        public String AdLinkUrl { get; set; }
        public String AdImgUrl { get; set; }
        public Type Type { get; set; }
        public DateTimeOffset Offset { get; set; }
    }

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

        //var json = model.ToJson();
        var json = _json.Write(model);

        //// 直接反序列化会抛出异常
        //Assert.Throws<Exception>(() => _json.Read(json, typeof(ModelA)));

        //// 上对象容器
        //ObjectContainer.Current.AddTransient<IDuck, DuckB>();

        // 再来一次反序列化
        //var model2 = json.ToJsonEntity<ModelA>();
        var model2 = _json.Read(json, typeof(ModelA)) as ModelA;
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
}