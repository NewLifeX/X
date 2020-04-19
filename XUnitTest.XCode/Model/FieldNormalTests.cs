using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class FieldNormalTests
    {
        [Fact]
        public void Equal()
        {
            var fi = Log._.Category;
            var exp = fi.Equal("登录");
            var where = exp.GetString(null);
            Assert.Equal("Category='登录'", where);
        }

        [Fact]
        public void Equal2()
        {
            var fi = Log._.Category;
            var exp = fi == "登录";
            var where = exp.GetString(null);
            Assert.Equal("Category='登录'", where);
        }

        [Fact]
        public void Equal2WithParameter()
        {
            var fi = Log._.Category;
            var exp = fi == "登录";
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(ps);
            Assert.Equal("Category=@Category", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Category"));
            Assert.Equal("登录", ps["Category"]);
        }

        [Fact]
        public void NotEqual()
        {
            var fi = Log._.Category;
            var exp = fi.NotEqual("登录");
            var where = exp.GetString(null);
            Assert.Equal("Category<>'登录'", where);
        }

        [Fact]
        public void NotEqual2()
        {
            var fi = Log._.Category;
            var exp = fi != "登录";
            var where = exp.GetString(null);
            Assert.Equal("Category<>'登录'", where);
        }

        [Fact]
        public void NotEqual2WithParameter()
        {
            var fi = Log._.Category;
            var exp = fi != "登录";
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(ps);
            Assert.Equal("Category<>@Category", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Category"));
            Assert.Equal("登录", ps["Category"]);
        }

        [Fact]
        public void IsNull()
        {
            var fi = Log._.Category;
            var exp = fi.IsNull();
            var where = exp.GetString(null);
            Assert.Equal("Category Is Null", where);
        }

        [Fact]
        public void NotIsNull()
        {
            var fi = Log._.Category;
            var exp = fi.NotIsNull();
            var where = exp.GetString(null);
            Assert.Equal("Not Category Is Null", where);
        }

        [Fact]
        public void IsNullOrEmpty()
        {
            var fi = Log._.Category;
            var exp = fi.IsNullOrEmpty();
            var where = exp.GetString(null);
            Assert.Equal("Category Is Null Or Category=''", where);
        }

        [Fact]
        public void NotIsNullOrEmpty()
        {
            var fi = Log._.Category;
            var exp = fi.NotIsNullOrEmpty();
            var where = exp.GetString(null);
            Assert.Equal("Not Category Is Null And Category<>''", where);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsTrue(Boolean? flag)
        {
            var fi = Log._.Success;
            var exp = fi.IsTrue(flag);
            var where = exp?.GetString(null);
            if (flag == null)
                Assert.Null(where);
            else if (flag.Value)
                Assert.Equal("Success=1", where);
            else
                Assert.Equal("Success<>1 Or Success Is Null", where);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsFalse(Boolean? flag)
        {
            var fi = Log._.Success;
            var exp = fi.IsFalse(flag);
            var where = exp?.GetString(null);
            if (flag == null)
                Assert.Null(where);
            else if (flag.Value)
                Assert.Equal("Success<>0 Or Success Is Null", where);
            else
                Assert.Equal("Success=0", where);
        }

        [Fact]
        public void LargerThen()
        {
            var fi = Area._.Level;
            var exp = fi > 1;
            var where = exp.GetString(null);
            Assert.Equal("Level>1", where);
        }

        [Fact]
        public void LargerOrEqual()
        {
            var fi = Area._.Level;
            var exp = fi >= 2;
            var where = exp.GetString(null);
            Assert.Equal("Level>=2", where);
        }

        [Fact]
        public void LessThen()
        {
            var fi = Area._.Level;
            var exp = fi < 3;
            var where = exp.GetString(null);
            Assert.Equal("Level<3", where);
        }

        [Fact]
        public void LessOrEqual()
        {
            var fi = Area._.Level;
            var exp = fi <= 2;
            var where = exp.GetString(null);
            Assert.Equal("Level<=2", where);
        }
    }
}