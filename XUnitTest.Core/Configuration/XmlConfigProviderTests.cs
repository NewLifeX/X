using System.IO;
using NewLife;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Configuration
{
    public class XmlConfigProviderTests
    {
        readonly IConfigProvider _provider;

        public XmlConfigProviderTests() => _provider = new XmlConfigProvider { FileName = "Config/core.xml" };

        [Fact]
        public void TestLoadAndSave()
        {
            var set = new ConfigModel
            {
                Debug = true,
                LogLevel = LogLevel.Fatal,
                LogPath = "xxx",
                NetworkLog = "255.255.255.255:514",
                TempPath = "yyy",

                Sys = new SysConfig
                {
                    Name = "NewLife.Cube",
                    DisplayName = "魔方平台",
                    Company = "新生命开发团队",
                },
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

            var sys = set.Sys;
            Assert.Equal(sys.Name, prv["Sys:Name"]);
            Assert.Equal(sys.DisplayName, prv["Sys:DisplayName"]);
            Assert.Equal(sys.Company, prv["Sys:Company"]);

            var set2 = _provider.Load<ConfigModel>();

            Assert.NotNull(set2);
            Assert.Equal(set.Debug, set2.Debug);
            Assert.Equal(set.LogLevel, set2.LogLevel);
            Assert.Equal(set.LogPath, set2.LogPath);
            Assert.Equal(set.NetworkLog, set2.NetworkLog);
            Assert.Equal(set.LogFileFormat, set2.LogFileFormat);
            Assert.Equal(set.TempPath, set2.TempPath);
            Assert.Equal(set.PluginPath, set2.PluginPath);
            Assert.Equal(set.PluginServer, set2.PluginServer);

            var sys2 = set2.Sys;
            Assert.NotNull(sys2);
            Assert.Equal(sys.Name, sys2.Name);
            Assert.Equal(sys.DisplayName, sys2.DisplayName);
            Assert.Equal(sys.Company, sys2.Company);
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

            var set = new ConfigModel();
            _provider.Bind(set, null);

            Assert.NotNull(set);
            Assert.True(set.Debug);
            Assert.Equal(LogLevel.Fatal, set.LogLevel);
            Assert.Equal("xxx", set.LogPath);
            Assert.Equal("255.255.255.255:514", set.NetworkLog);
        }
    }
}