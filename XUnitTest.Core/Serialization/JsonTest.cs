using System;
using System.Collections;
using System.Collections.Generic;
using NewLife;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization
{
    public class JsonTest
    {
        [Fact(DisplayName = "基础测试")]
        public void Test1()
        {
            var set = new Setting
            {
                LogLevel = LogLevel.Error,
                LogPath = "xxx",
            };

            var js = set.ToJson(true, false, false);
            Assert.True(js.StartsWith("{") && js.EndsWith("}"));

            var set2 = js.ToJsonEntity<Setting>();

            Assert.Equal(LogLevel.Error, set2.LogLevel);
            Assert.Equal("xxx", set2.LogPath);
        }

        [Fact]
        public void DateTimeTest()
        {
            var str = @"[
    {
        ""ID"": 0,
        ""Userid"": 27,
        ""ClickTime"": ""2020-03-09T21:16:17.88"",
        ""AdID"": 39,
        ""AdAmount"": 0.43,
        ""isGive"": false,
        ""AdLinkUrl"": ""http://www.baidu.com"",
        ""AdImgUrl"": ""/uploader/swiperPic/405621836.jpg""
    },
    {
        ""ID"": 0,
        ""Userid"": 27,
        ""ClickTime"": ""2020-03-09T21:16:25.9052764+08:00"",
        ""AdID"": 40,
        ""AdAmount"": 0.41,
        ""isGive"": false,
        ""AdLinkUrl"": ""http://www.baidu.com"",
        ""AdImgUrl"": ""/uploader/swiperPic/1978468752.jpg""
    }
]";

            var models = str.ToJsonEntity<Model[]>();
            Assert.Equal(2, models.Length);

            var m = models[0];
            Assert.Equal(27, m.UserId);
            Assert.Equal(new DateTime(2020, 3, 9, 21, 16, 17, 880), m.ClickTime);
            Assert.Equal(39, m.AdId);
            Assert.Equal(0.43, m.AdAmount);
            Assert.False(m.IsGive);
            Assert.Equal("http://www.baidu.com", m.AdLinkUrl);
            Assert.Equal("/uploader/swiperPic/405621836.jpg", m.AdImgUrl);
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

            var json = model.ToJson();

            // 直接反序列化会抛出异常
            Assert.Throws<Exception>(() => json.ToJsonEntity<ModelA>());

            // 上对象容器
            ObjectContainer.Current.AddTransient<IDuck, DuckB>();

            // 再来一次反序列化
            var model2 = json.ToJsonEntity<ModelA>();
            Assert.NotNull(model2);
            Assert.Equal(2233, model2.ID);
            Assert.Equal(3, model2.Childs.Count);
            Assert.Equal("123", model.Childs[0].Name);
            Assert.Equal("456", model.Childs[1].Name);
            Assert.Equal("789", model.Childs[2].Name);
        }

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
            var json = model.ToJson();

            // 反序列化
            var model2 = json.ToJsonEntity<ModelB>();
            Assert.NotNull(model2);
            Assert.Equal(2233, model2.ID);

            var dic = model2.Body as IDictionary<String, Object>;
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
    }
}