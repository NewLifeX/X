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

            xml.Save(cfg);
            json.Save(cfg);
        }
    }
}