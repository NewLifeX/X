using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife;
using NewLife.Configuration;
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
        public void TestLoad()
        {
            var set = _provider.Load<Setting>();
            Assert.NotNull(set);
            Assert.True(set.Debug);
            Assert.NotEmpty(set.LogFileFormat);
        }

        [Fact]
        public void TestSave()
        {
            var set = new Setting();
            _provider.Save(set);

            Assert.NotNull(set);
            Assert.True(set.Debug);
            Assert.NotEmpty(set.LogFileFormat);
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