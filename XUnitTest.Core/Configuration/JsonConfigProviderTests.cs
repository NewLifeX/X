using System.IO;
using NewLife;
using NewLife.Configuration;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Configuration
{
    public class JsonConfigProviderTests
    {
        IConfigProvider _provider;

        public JsonConfigProviderTests()
        {
            var provider = new JsonConfigProvider { FileName = "Config/core.json" };
            _provider = provider;

            var json = @"{
              ""Debug"":  ""True"",
              ""LogLevel"":  ""Info"",
              ""LogPath"":  """",
              ""NetworkLog"":  """",
              ""LogFileFormat"":  ""{0:yyyy_MM_dd}.log"",
              ""TempPath"":  """",
              ""PluginPath"":  ""Plugins"",
              ""PluginServer"":  ""http://x.newlifex.com/"",
            }";

            var file = provider.FileName.GetFullPath();
            if (!File.Exists(file)) File.WriteAllText(file, json);
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
            Assert.Equal(prv["Debug"], set.Debug + "");
            Assert.Equal(prv["LogLevel"], set.LogLevel + "");
            Assert.Equal(prv["LogPath"], set.LogPath + "");
            Assert.Equal(prv["NetworkLog"], set.NetworkLog + "");
            Assert.Equal(prv["LogFileFormat"], set.LogFileFormat + "");
            Assert.Equal(prv["TempPath"], set.TempPath + "");
            Assert.Equal(prv["PluginPath"], set.PluginPath + "");
            Assert.Equal(prv["PluginServer"], set.PluginServer + "");

            var set2 = _provider.Load<Setting>();

            Assert.NotNull(set2);
            Assert.Equal(set2.Debug, set.Debug);
            Assert.Equal(set2.LogLevel, set.LogLevel);
            Assert.Equal(set2.LogPath, set.LogPath);
            Assert.Equal(set2.NetworkLog, set.NetworkLog);
            Assert.Equal(set2.LogFileFormat, set.LogFileFormat);
            Assert.Equal(set2.TempPath, set.TempPath);
            Assert.Equal(set2.PluginPath, set.PluginPath);
            Assert.Equal(set2.PluginServer, set.PluginServer);
        }

        [Fact]
        public void TestBind()
        {
            var set = new Setting();
            _provider.Bind(set, null);

            Assert.NotNull(set);
            Assert.True(set.Debug);
            Assert.NotEmpty(set.LogFileFormat);
        }
    }
}