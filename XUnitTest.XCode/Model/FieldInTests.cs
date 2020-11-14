using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class FieldInTests
    {
        private IDatabase _dbUser;
        private IDatabase _dbLog;
        public FieldInTests()
        {
            _dbUser = User.Meta.Session.Dal.Db;
            _dbLog = Log.Meta.Session.Dal.Db;
        }

        [Fact]
        public void In()
        {
            var fi = User._.RoleID;
            var exp = fi.In(new[] { 1, 2, 3, 4 });
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleID In(1,2,3,4)", where);

            Assert.Equal("RoleID In(1,2,3,4)", exp);

            Assert.Throws<ArgumentNullException>(() => fi.In((IEnumerable)null));
            Assert.Throws<ArgumentNullException>(() => fi.In(new Int32[0]));
            Assert.Throws<ArgumentNullException>(() => fi.In(new Object[0]));
        }

        [Fact]
        public void InForStringArray()
        {
            var fi = Log._.Category;
            var exp = fi.In(new[] { "登录", "注册", "同步" });
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category In('登录','注册','同步')", where);

            Assert.Equal("Category In('登录','注册','同步')", exp);

            Assert.Throws<ArgumentNullException>(() => fi.In((IEnumerable)null));
            Assert.Throws<ArgumentNullException>(() => fi.In(new String[0]));
            Assert.Throws<ArgumentNullException>(() => fi.In(new Object[0]));
        }

        [Fact]
        public void InForString()
        {
            var fi = Log._.Category;
            var exp = fi.In("登录,注册,同步");
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category In('登录','注册','同步')", where);

            Assert.Equal("Category In('登录','注册','同步')", exp);

            Assert.Throws<ArgumentNullException>(() => fi.In((String)null));
            Assert.Throws<ArgumentNullException>(() => fi.In(""));
        }

        [Fact]
        public void NotIn()
        {
            var fi = User._.RoleID;
            var exp = fi.NotIn(new[] { 1, 2, 3, 4 });
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleID Not In(1,2,3,4)", where);

            Assert.Equal("RoleID Not In(1,2,3,4)", exp);

            Assert.Throws<ArgumentNullException>(() => fi.NotIn((IEnumerable)null));
            Assert.Throws<ArgumentNullException>(() => fi.NotIn(new Int32[0]));
            Assert.Throws<ArgumentNullException>(() => fi.NotIn(new Object[0]));
        }

        [Fact]
        public void NotInForStringArray()
        {
            var fi = Log._.Category;
            var exp = fi.NotIn(new[] { "登录", "注册", "同步" });
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category Not In('登录','注册','同步')", where);

            Assert.Equal("Category Not In('登录','注册','同步')", exp);

            Assert.Throws<ArgumentNullException>(() => fi.NotIn((IEnumerable)null));
            Assert.Throws<ArgumentNullException>(() => fi.NotIn(new String[0]));
            Assert.Throws<ArgumentNullException>(() => fi.NotIn(new Object[0]));
        }

        [Fact]
        public void NotInForString()
        {
            var fi = Log._.Category;
            var exp = fi.NotIn("登录,注册,同步");
            var where = exp.GetString(_dbLog, null);
            Assert.Equal("Category Not In('登录','注册','同步')", where);

            Assert.Equal("Category Not In('登录','注册','同步')", exp);

            Assert.Throws<ArgumentNullException>(() => fi.NotIn((String)null));
            Assert.Throws<ArgumentNullException>(() => fi.NotIn(""));
        }

        [Fact]
        public void InForSelectBuilder()
        {
            var fi = User._.RoleID;
            var exp = fi.In(Role.FindSQLWithKey());
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleID In(Select ID From Role)", where);

            Assert.Equal("RoleID In(Select ID From Role)", exp);

            Assert.Throws<ArgumentNullException>(() => fi.In((SelectBuilder)null));
        }

        [Fact]
        public void NotInForSelectBuilder()
        {
            var fi = User._.RoleID;
            var exp = fi.NotIn(Role.FindSQLWithKey());
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleID Not In(Select ID From Role)", where);

            Assert.Equal("RoleID Not In(Select ID From Role)", exp);

            Assert.Throws<ArgumentNullException>(() => fi.NotIn((SelectBuilder)null));
        }
    }
}