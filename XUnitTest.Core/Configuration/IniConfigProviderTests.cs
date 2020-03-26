using System.IO;
using NewLife;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Configuration
{
    public class InIConfigProviderTests
    {
        readonly IConfigProvider _provider;

        public InIConfigProviderTests() => _provider = new InIConfigProvider { FileName = "Config/core1.ini" };

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

            var prv = _provider;
            Assert.NotNull(prv);
            Assert.Equal(set.Debug.ToString().ToLower(), prv["Debug"]);
            Assert.Equal(set.LogLevel + "", prv["LogLevel"]);
            Assert.Equal(set.LogPath, prv["LogPath"]);
            Assert.Equal(set.NetworkLog, prv["NetworkLog"]);
            Assert.Equal(set.LogFileFormat, prv["LogFileFormat"]);
            Assert.Equal(set.TempPath, prv["TempPath"]);
            Assert.Equal(set.PluginPath, prv["PluginPath"]);
            Assert.Equal(set.PluginServer, prv["PluginServer"]);

            Assert.Equal("全局调试。XTrace.Debug", prv.GetSection("Debug").Comment);
            Assert.Equal("系统配置", prv.GetSection("Sys").Comment);
            Assert.Equal("用于标识系统的英文名", prv.GetSection("Sys:Name").Comment);

            var sys = set.Sys;
            Assert.Equal(sys.Name, prv["Sys:Name"]);
            Assert.Equal(sys.DisplayName, prv["Sys:DisplayName"]);
            Assert.Equal(sys.Company, prv["Sys:Company"]);

            var prv2 = new InIConfigProvider { FileName = (_provider as FileConfigProvider).FileName };
            var set2 = prv2.Load<ConfigModel>();

            Assert.NotNull(set2);
            Assert.Equal(set.Debug, set2.Debug);
            Assert.Equal(set.LogLevel, set2.LogLevel);
            Assert.Equal(set.LogPath, set2.LogPath);
            Assert.Equal(set.NetworkLog, set2.NetworkLog);
            Assert.Equal(set.LogFileFormat, set2.LogFileFormat);
            Assert.Equal(set.TempPath, set2.TempPath);
            Assert.Equal(set.PluginPath, set2.PluginPath);
            Assert.Equal(set.PluginServer, set2.PluginServer);

            Assert.Equal("全局调试。XTrace.Debug", prv2.GetSection("Debug").Comment);
            Assert.Equal("系统配置", prv2.GetSection("Sys").Comment);
            Assert.Equal("用于标识系统的英文名", prv2.GetSection("Sys:Name").Comment);

            var sys2 = set2.Sys;
            Assert.NotNull(sys2);
            Assert.Equal(sys.Name, sys2.Name);
            Assert.Equal(sys.DisplayName, sys2.DisplayName);
            Assert.Equal(sys.Company, sys2.Company);
        }

        [Fact]
        public void TestBind()
        {
            var json = @"Debug = True
LogLevel = Fatal
LogPath = xxx
NetworkLog = 255.255.255.255:514
LogFileFormat = {0:yyyy_MM_dd}.log
TempPath = yyy
PluginPath = Plugins
PluginServer = http://x.newlifex.com/

[Sys]
Name = NewLife.Cube
Version = 
DisplayName = 魔方平台
Company = 新生命开发团队
Develop = True
Enable = True
InstallTime = 2019-12-30 18:26:18
";

            var prv = new InIConfigProvider { FileName = "Config/core2.ini" };
            var file = prv.FileName.GetFullPath();
            File.WriteAllText(file, json);
            prv.LoadAll();

            var set = new ConfigModel();
            prv.Bind(set);

            Assert.NotNull(set);
            Assert.True(set.Debug);
            Assert.Equal(LogLevel.Fatal, set.LogLevel);
            Assert.Equal("xxx", set.LogPath);
            Assert.Equal("255.255.255.255:514", set.NetworkLog);

            // 保存
            prv.Save(set);
        }
    }
}