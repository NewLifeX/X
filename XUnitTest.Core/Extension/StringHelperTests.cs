using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NewLife;

namespace XUnitTest.Extension
{
    public class StringHelperTests
    {
        [Fact]
        public void IsMatch()
        {
            var rs = "".IsMatch("Stone");
            Assert.False(rs);

            rs = "*.zip".IsMatch(null);
            Assert.False(rs);

            // 常量
            rs = ".zip".IsMatch(".zip");
            Assert.True(rs);
            rs = ".zip".IsMatch("7788.Zip");
            Assert.False(rs);
            rs = ".zip".IsMatch(".Zip", StringComparison.OrdinalIgnoreCase);
            Assert.True(rs);
            rs = "/".IsMatch("/");
            Assert.True(rs);
            rs = "/".IsMatch("/api");
            Assert.False(rs);
            rs = "/".IsMatch("/api/");
            Assert.False(rs);

            // 头部
            rs = "*.zip".IsMatch("7788.zip");
            Assert.True(rs);
            rs = "*.zip".IsMatch("7788.zipxx");
            Assert.False(rs);

            // 大小写
            rs = "*.zip".IsMatch("4455.Zip");
            Assert.False(rs);
            rs = "*.zip".IsMatch("4455.Zip", StringComparison.OrdinalIgnoreCase);
            Assert.True(rs);

            // 中间
            rs = "build*.zip".IsMatch("build7788.zip");
            Assert.True(rs);
            rs = "build*.zip".IsMatch("mybuild7788.zip");
            Assert.False(rs);
            rs = "build*.zip".IsMatch("build7788.zipxxx");
            Assert.False(rs);

            // 尾部
            rs = "build.*".IsMatch("build.zip");
            Assert.True(rs);
            rs = "build.*".IsMatch("mybuild.zip");
            Assert.False(rs);
            rs = "build.*".IsMatch("build.zipxxx");
            Assert.True(rs);

            // 多个
            rs = "build*.*".IsMatch("build7788.zip");
            Assert.True(rs);
            rs = "*build*.*".IsMatch("mybuild7788.zip");
            Assert.True(rs);
            rs = "build**.*".IsMatch("build7788.zip");
            Assert.True(rs);

            // 其它
            rs = "aa*aa".IsMatch("aaa");
            Assert.False(rs);
            rs = "aa*aa".IsMatch("aaaa");
            Assert.True(rs);
            rs = "aa*aa".IsMatch("aaaaa");
            Assert.True(rs);
        }
    }
}