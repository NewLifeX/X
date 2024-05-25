using System;
using NewLife;
using Xunit;

namespace XUnitTest.Serialization;

public abstract class JsonTestBase
{
    protected static String _json_value;
    static JsonTestBase()
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
                    "Offset": "2022-11-29T14:13:17.8763881+08:00",
                    "Date": "2022-11-29",
                    "Time": "14:13:17.8763881",
                    "Roles": ["admin", "user"],
                    "Scores": [1, 2, 3]
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
                    "Offset": "2022-11-29T14:13:17.8763881+08:00",
                    "Date": "2022-11-29",
                    "Time": "14:13:17.8763881",
                    "Roles": ["admin", "user"],
                    "Scores": [1, 2, 3]
                }
            ]
            """;
        _json_value = str;
    }

    protected void CheckModel(Model[] models)
    {
        var m = models[0];
        Assert.Equal(27, m.UserId);

        var dt = new DateTime(2020, 3, 9, 21, 16, 17, 880);
        Assert.Equal(dt, m.ClickTime);
        Assert.Equal(39, m.AdId);
        Assert.Equal(0.43, m.AdAmount);
        Assert.False(m.IsGive);
        Assert.Equal("http://www.baidu.com", m.AdLinkUrl);
        Assert.Equal("/uploader/swiperPic/405621836.jpg", m.AdImgUrl);
        Assert.Equal(typeof(NewLife.Common.PinYin), m.Type);
        Assert.Equal(DateTimeOffset.Parse("2022-11-29T14:13:17.8763881+08:00"), m.Offset);
        Assert.Equal(DateOnly.Parse("2022-11-29"), m.Date);
        Assert.Equal(TimeOnly.Parse("14:13:17.8763881"), m.Time);
        Assert.Equal("admin,user", m.Roles?.Join());
        Assert.Equal("1,2,3", m.Scores?.Join());

        m = models[1];
        Assert.Equal(27, m.UserId);

        var dto = "2020-03-09T21:16:25.9052764+08:00".ToDateTimeOffset();
        Assert.Equal(dto.Trim("us"), m.ClickTime.Trim("us"));
        Assert.Equal(40, m.AdId);
        Assert.Equal(0.41, m.AdAmount);
        Assert.False(m.IsGive);
        Assert.Equal("http://www.baidu.com", m.AdLinkUrl);
        Assert.Equal("/uploader/swiperPic/1978468752.jpg", m.AdImgUrl);
        Assert.Equal(typeof(String), m.Type);
        Assert.Equal(DateTimeOffset.Parse("2022-11-29T14:13:17.8763881+08:00"), m.Offset);
        Assert.Equal(DateOnly.Parse("2022-11-29"), m.Date);
        Assert.Equal(TimeOnly.Parse("14:13:17.8763881"), m.Time);
        Assert.Equal("admin,user", m.Roles?.Join());
        Assert.Equal("1,2,3", m.Scores?.Join());
    }

    protected class Model
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
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public String[] Roles { get; set; }
        public Int32[] Scores { get; set; } = [];
    }
}
