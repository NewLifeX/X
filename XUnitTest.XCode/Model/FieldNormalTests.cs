using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class FieldNormalTests
    {
        private IDatabase _dbUser;
        private IDatabase _dbLog;
        private IDatabase _dbArea;
        public FieldNormalTests()
        {
            _dbUser = User.Meta.Session.Dal.Db;
            _dbLog = Log.Meta.Session.Dal.Db;
            _dbArea = Area.Meta.Session.Dal.Db;
        }

        [Fact]
        public void Equal()
        {
            var fi = Log._.Category;
            var exp = fi.Equal("登录");
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category='登录'", where);

            Assert.Equal("Category='登录'", exp);
        }

        [Fact]
        public void Equal2()
        {
            var fi = Log._.Category;
            var exp = fi == "登录";
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category='登录'", where);

            Assert.Equal("Category='登录'", exp);
        }

        [Fact]
        public void Equal2WithParameter()
        {
            var fi = Log._.Category;
            var exp = fi == "登录";
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbLog, ps);
            Assert.Equal("Category=@Category", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Category"));
            Assert.Equal("登录", ps["Category"]);

            Assert.Equal("Category='登录'", exp);
        }

        [Fact]
        public void NotEqual()
        {
            var fi = Log._.Category;
            var exp = fi.NotEqual("登录");
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category<>'登录'", where);

            Assert.Equal("Category<>'登录'", exp);
        }

        [Fact]
        public void NotEqual2()
        {
            var fi = Log._.Category;
            var exp = fi != "登录";
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category<>'登录'", where);

            Assert.Equal("Category<>'登录'", exp);
        }

        [Fact]
        public void NotEqual2WithParameter()
        {
            var fi = Log._.Category;
            var exp = fi != "登录";
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbLog, ps);
            Assert.Equal("Category<>@Category", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Category"));
            Assert.Equal("登录", ps["Category"]);

            Assert.Equal("Category<>'登录'", exp);
        }

        [Fact]
        public void IsNull()
        {
            var fi = Log._.Category;
            var exp = fi.IsNull();
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category Is Null", where);

            Assert.Equal("Category Is Null", exp);
        }

        [Fact]
        public void NotIsNull()
        {
            var fi = Log._.Category;
            var exp = fi.NotIsNull();
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Not Category Is Null", where);

            Assert.Equal("Not Category Is Null", exp);
        }

        [Fact]
        public void IsNullOrEmpty()
        {
            var fi = Log._.Category;
            var exp = fi.IsNullOrEmpty();
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category Is Null Or Category=''", where);

            Assert.Equal("Category Is Null Or Category=''", exp);
        }

        [Fact]
        public void NotIsNullOrEmpty()
        {
            var fi = Log._.Category;
            var exp = fi.NotIsNullOrEmpty();
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Not Category Is Null And Category<>''", where);

            Assert.Equal("Not Category Is Null And Category<>''", exp);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsTrue(Boolean? flag)
        {
            var fi = Log._.Success;
            var exp = fi.IsTrue(flag);
            var where = exp?.GetString(_dbLog, null);
            if (flag == null)
            {
                Assert.Null(where);
                Assert.Null(exp?.ToString());
            }
            else if (flag.Value)
            {
                Assert.Equal("Success=1", where);
                Assert.Equal("Success=1", exp);
            }
            else
            {
                Assert.Equal("Success<>1 Or Success Is Null", where);
                Assert.Equal("Success<>1 Or Success Is Null", exp);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void IsFalse(Boolean? flag)
        {
            var fi = Log._.Success;
            var exp = fi.IsFalse(flag);
            var where = exp?.GetString(_dbLog, null);
            if (flag == null)
            {
                Assert.Null(where);
                Assert.Null(exp?.ToString());
            }
            else if (flag.Value)
            {
                Assert.Equal("Success<>0 Or Success Is Null", where);
                Assert.Equal("Success<>0 Or Success Is Null", exp);
            }
            else
            {
                Assert.Equal("Success=0", where);
                Assert.Equal("Success=0", exp);
            }
        }

        [Fact]
        public void LargerThen()
        {
            var fi = Area._.Level;
            var exp = fi > 1;
            var where = exp.GetString(_dbArea, null);
            Assert.Equal("Level>1", where);

            Assert.Equal("Level>1", exp);
        }

        [Fact]
        public void LargerOrEqual()
        {
            var fi = Area._.Level;
            var exp = fi >= 2;
            var where = exp.GetString(_dbArea, null);
            Assert.Equal("Level>=2", where);

            Assert.Equal("Level>=2", exp);
        }

        [Fact]
        public void LessThen()
        {
            var fi = Area._.Level;
            var exp = fi < 3;
            var where = exp.GetString(_dbArea, null);
            Assert.Equal("Level<3", where);

            Assert.Equal("Level<3", exp);
        }

        [Fact]
        public void LessOrEqual()
        {
            var fi = Area._.Level;
            var exp = fi <= 2;
            var where = exp.GetString(_dbArea, null);
            Assert.Equal("Level<=2", where);

            Assert.Equal("Level<=2", exp);
        }
    }
}