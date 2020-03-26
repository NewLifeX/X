using System;
using System.Collections.Generic;
using System.Text;
using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration
{
    public class ConfigProviderTests
    {
        [Fact]
        public void Test1()
        {
            var ini = new InIConfigProvider { FileName = "Config/core0.ini" };
            var xml = new XmlConfigProvider { FileName = "Config/core0.xml" };
            var json = new JsonConfigProvider { FileName = "Config/core0.json" };
            var http = new HttpConfigProvider
            {
                Server = "http://127.0.0.1:5000/config,http://10.0.0.4/config",
                AppKey = "Test",
                Secret = "12345678",
                LocalCache = true,
            };

            var p = http["LogPath"];
            http["LogPath"] = p;

            var cfg = http.Load<Setting>();

            Assert.NotNull(cfg);
            Assert.True(cfg.Debug);
            Assert.NotEmpty(cfg.LogFileFormat);

            ini.Save(cfg);
            xml.Save(cfg);
            json.Save(cfg);
        }

        [Fact]
        public void Find()
        {
            var ini = new InIConfigProvider();

            Assert.Null(ini["xxx"]);
            Assert.Null(ini["aaa"]);
            Assert.Null(ini["aaa:bbb:ccc"]);

            ini["xxx"] = "xyz";
            var ci = ini.GetSection("xxx");
            Assert.NotNull(ci);
            Assert.Equal("xxx", ci.Key);
            Assert.Equal("xyz", ci.Value);

            ini["aaa:bbb:ccc"] = "abc";
            var ci2 = ini.GetSection("aaa");
            Assert.NotNull(ci2);
            Assert.Equal("aaa", ci2.Key);
            Assert.Null(ci2.Value);
            Assert.NotNull(ci2.Childs);
            Assert.True(ci2.Childs.Count == 1);

            ci2 = ci2.Childs[0];
            Assert.NotNull(ci2);
            Assert.Equal("bbb", ci2.Key);
            Assert.Null(ci2.Value);
            Assert.NotNull(ci2.Childs);
            Assert.True(ci2.Childs.Count == 1);

            ci2 = ci2.Childs[0];
            Assert.NotNull(ci2);
            Assert.Equal("ccc", ci2.Key);
            Assert.Equal("abc", ci2.Value);
            Assert.Null(ci2.Childs);

            var ci3 = ini.GetSection("aaa:bbb:ccc");
            Assert.NotNull(ci3);
            Assert.Equal("ccc", ci3.Key);
            Assert.Equal("abc", ci3.Value);
            Assert.Equal(ci2, ci3);
        }

        public void GetSet()
        {

        }
    }
}