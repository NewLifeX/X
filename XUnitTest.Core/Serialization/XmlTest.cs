using NewLife;
using NewLife.Log;
using NewLife.Xml;
using Xunit;

namespace XUnitTest.Serialization
{
    public class XmlTest
    {
        [Fact(DisplayName = "基础测试")]
        public void Test1()
        {
            var set = new Setting
            {
                LogLevel = LogLevel.Error,
                LogPath = "xxx",
            };

            var xml = set.ToXml();
            Assert.Contains("<Setting>", xml);
            Assert.Contains("</Setting>", xml);

            var xml2 = set.ToXml(null, false, true);
            Assert.Contains("<Setting ", xml2);

            var set2 = xml.ToXmlEntity<Setting>();

            Assert.Equal(LogLevel.Error, set2.LogLevel);
            Assert.Equal("xxx", set2.LogPath);
        }
    }
}