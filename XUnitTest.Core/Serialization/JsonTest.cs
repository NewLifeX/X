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
    }
}