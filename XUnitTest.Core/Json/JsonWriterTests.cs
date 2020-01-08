using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Json
{
    public class JsonWriterTests
    {
        [Fact]
        public void UtcTest()
        {
            var writer = new JsonWriter();

            var dt = DateTime.UtcNow;
            writer.Write(new { time = dt });

            var str = writer.GetString();
            Assert.NotEmpty(str);

            var js = new JsonParser(str);
            var dic = js.Decode() as IDictionary<String, Object>;
            Assert.NotNull(dic);

            var str2 = dic["time"];
            Assert.Equal(dt.ToFullString(), str2);

            var dt2 = dic["time"].ToDateTime();
            Assert.Equal(dt.Trim(), dt2.Trim());
        }

        [Fact]
        public void EnumTest()
        {
            // 字符串
            var writer = new JsonWriter { EnumString = true };

            var data = new { Level = LogLevel.Fatal };
            writer.Write(data);

            var js = new JsonParser(writer.GetString());
            var dic = js.Decode() as IDictionary<String, Object>;
            Assert.NotNull(dic);

            var str2 = dic["Level"];
            Assert.Equal("Fatal", str2);

            // 数字
            var writer2 = new JsonWriter { EnumString = false };

            writer2.Write(data);

            var js2 = new JsonParser(writer2.GetString());
            var dic2 = js2.Decode() as IDictionary<String, Object>;
            Assert.NotNull(dic2);

            Assert.Equal(5, dic2["Level"]);
            Assert.Equal((Int32)LogLevel.Fatal, dic2["Level"].ToInt());
        }
    }
}