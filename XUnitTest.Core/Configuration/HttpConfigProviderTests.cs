using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration
{
    public class HttpConfigProviderTests
    {
        private readonly String _server;
        public HttpConfigProviderTests()
        {
            var file = "config/http.config".GetFullPath();
            if (File.Exists(file))
                _server = File.ReadAllText(file);
            else
            {
                _server = "http://127.0.0.1:7080,http://10.0.0.1:7080";
                File.WriteAllText(file, _server);
            }
        }

        [Fact]
        public void TestApollo()
        {
            var provider = new HttpConfigProvider
            {
                Server = _server,
                AppId = "testapi"
            };
            provider.SetApollo("application");
            //provider.LoadAll();

            var url = provider["appapiurl"];
            Assert.NotEmpty(url);

            var keys = provider.Keys.ToArray();
            Assert.NotNull(keys);
        }
    }
}