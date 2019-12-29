using System;
using System.Collections.Generic;
using System.Text;
using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration
{
    public class XmlConfigProviderTests
    {
        IConfigProvider _provider;

        public XmlConfigProviderTests()
        {
            _provider = new XmlConfigProvider { FileName = "Config/core.config" };

            var set = Setting.Current;
        }

        [Fact]
        public void TestGet()
        {
            //var set = new Setting();
            var set = _provider.Load<Setting>();
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