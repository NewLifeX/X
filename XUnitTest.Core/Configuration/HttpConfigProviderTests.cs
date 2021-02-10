using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
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

            var model = provider.Load<Model>();
            Assert.NotNull(model);
            Assert.NotEmpty(model.AppApiUrl);
            Assert.Equal(url, model.AppApiUrl);
            Assert.True(model.Radius > 0);
            Assert.NotEmpty(model.MySqlServer);

            var model2 = new Model();
            provider.Bind(model2);
            Assert.Equal(url, model2.AppApiUrl);
            Assert.True(model2.Radius > 0);
            Assert.NotEmpty(model2.MySqlServer);
        }

        private class Model
        {
            public Int32 Radius { get; set; }

            public String MySqlServer { get; set; }

            public String AppApiUrl { get; set; }
        }

        [Fact]
        public void TestStardust()
        {
            var provider = new HttpConfigProvider
            {
                Server = "http://star.newlifex.com:6600",
                //Server = "http://localhost:6600",
                AppId = "StarWeb"
            };

            var str = provider["test1"];
            Assert.NotEmpty(str);

            var keys = provider.Keys.ToArray();
            Assert.NotNull(keys);

            var model = provider.Load<Model2>();
            Assert.NotNull(model);
            Assert.NotEmpty(model.Test);
            Assert.Equal(str, model.Test);
            Assert.NotEmpty(model.Shop);
            Assert.NotEmpty(model.Title);
            Assert.Equal("NewLife开发团队", model.Title);

            var model2 = new Model2();
            provider.Bind(model2);
            Assert.Equal(str, model2.Test);
            Assert.NotEmpty(model.Shop);
            Assert.Equal("NewLife开发团队", model.Title);
        }

        private class Model2
        {
            [DataMember(Name = "test1")]
            public String Test { get; set; }

            [DataMember(Name = "conn_Shop")]
            public String Shop { get; set; }

            public String Title { get; set; }
        }
    }
}