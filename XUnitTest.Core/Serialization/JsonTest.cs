using System;
using NewLife;
using NewLife.Log;
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
    }
}