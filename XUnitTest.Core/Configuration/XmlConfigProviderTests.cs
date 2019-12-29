using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife;
using NewLife.Configuration;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Configuration
{
    public class XmlConfigProviderTests
    {
        IConfigProvider _provider;

        public XmlConfigProviderTests()
        {
            _provider = new XmlConfigProvider { FileName = "Config/core.xml" };
        }

        [Fact]
        public void TestLoadAndSave()
        {
            var set = new Setting
            {
                Debug = true,
                LogLevel = LogLevel.Fatal,
                LogPath = "xxx",
                NetworkLog = "255.255.255.255:514",
                TempPath = "yyy",
            };

            _provider.Save(set);

            var prv = _provider as FileConfigProvider;
            Assert.NotNull(prv);
            Assert.Equal(set.Debug + "", prv["Debug"]);
            Assert.Equal(set.LogLevel + "", prv["LogLevel"]);
            Assert.Equal(set.LogPath, prv["LogPath"]);
            Assert.Equal(set.NetworkLog, prv["NetworkLog"]);
            Assert.Equal(set.LogFileFormat, prv["LogFileFormat"]);
            Assert.Equal(set.TempPath, prv["TempPath"]);
            Assert.Equal(set.PluginPath, prv["PluginPath"]);
            Assert.Equal(set.PluginServer, prv["PluginServer"]);

            var set2 = _provider.Load<Setting>();

            Assert.NotNull(set2);
            Assert.Equal(set.Debug, set2.Debug);
            Assert.Equal(set.LogLevel, set2.LogLevel);
            Assert.Equal(set.LogPath, set2.LogPath);
            Assert.Equal(set.NetworkLog, set2.NetworkLog);
            Assert.Equal(set.LogFileFormat, set2.LogFileFormat);
            Assert.Equal(set.TempPath, set2.TempPath);
            Assert.Equal(set.PluginPath, set2.PluginPath);
            Assert.Equal(set.PluginServer, set2.PluginServer);
        }

        [Fact]
        public void TestBind()
        {
            var json = @"<?xml version=""1.0"" encoding=""utf-8""?>
<core>
  <Debug>True</Debug>
  <LogLevel>Fatal</LogLevel>
  <LogPath>xxx</LogPath>
  <NetworkLog>255.255.255.255:514</NetworkLog>
  <LogFileFormat>{0:yyyy_MM_dd}.log</LogFileFormat>
  <TempPath>yyy</TempPath>
  <PluginPath>Plugins</PluginPath>
  <PluginServer>http://x.newlifex.com/</PluginServer>
</core>";

            var prv = _provider as FileConfigProvider;
            var file = prv.FileName.GetFullPath();
            File.WriteAllText(file, json);

            var set = new Setting();
            _provider.Bind(set, null);

            Assert.NotNull(set);
            Assert.True(set.Debug);
            Assert.Equal(LogLevel.Fatal, set.LogLevel);
            Assert.Equal("xxx", set.LogPath);
            Assert.Equal("255.255.255.255:514", set.NetworkLog);
        }
    }
}