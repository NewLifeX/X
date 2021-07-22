using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace XUnitTest.IO
{
    public class PathHelperTests
    {
        [Fact]
        public void BasePath()
        {
            var bpath = PathHelper.BasePath;

            Assert.NotEmpty(bpath);
            Assert.Equal(bpath, AppDomain.CurrentDomain.BaseDirectory);

            Assert.Equal("config".GetFullPath(), "config".GetBasePath());

            // 改变
            PathHelper.BasePath = "../xx";
            Assert.Equal("../xx/config".GetFullPath(), "config".GetBasePath());

            PathHelper.BasePath = bpath;
        }
    }
}