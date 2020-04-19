using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class FieldInTests
    {
        [Fact]
        public void In()
        {
            var fi = UserX._.RoleID;
            var exp = fi.In(new[] { 1, 2, 3, 4 });
            var where = exp.GetString(null);
            Assert.Equal("RoleID In(1,2,3,4)", where);
        }

        [Fact]
        public void InForStringArray()
        {
            var fi = Log._.Category;
            var exp = fi.In(new[] { "登录", "注册", "同步" });
            var where = exp.GetString(null);
            Assert.Equal("Category In('登录','注册','同步')", where);
        }

        [Fact]
        public void InForString()
        {
            var fi = Log._.Category;
            var exp = fi.In("登录,注册,同步");
            var where = exp.GetString(null);
            Assert.Equal("Category In('登录','注册','同步')", where);
        }

        [Fact]
        public void NotIn()
        {
            var fi = UserX._.RoleID;
            var exp = fi.NotIn(new[] { 1, 2, 3, 4 });
            var where = exp.GetString(null);
            Assert.Equal("RoleID Not In(1,2,3,4)", where);
        }

        [Fact]
        public void NotInForStringArray()
        {
            var fi = Log._.Category;
            var exp = fi.NotIn(new[] { "登录", "注册", "同步" });
            var where = exp.GetString(null);
            Assert.Equal("Category Not In('登录','注册','同步')", where);
        }

        [Fact]
        public void NotInForString()
        {
            var fi = Log._.Category;
            var exp = fi.NotIn("登录,注册,同步");
            var where = exp.GetString(null);
            Assert.Equal("Category Not In('登录','注册','同步')", where);
        }

        [Fact]
        public void InForSelectBuilder()
        {
            var fi = UserX._.RoleID;
            var exp = fi.In(Role.FindSQLWithKey());
            var where = exp.GetString(null);
            Assert.Equal("RoleID In(Select ID From Role)", where);
        }

        [Fact]
        public void NotInForSelectBuilder()
        {
            var fi = UserX._.RoleID;
            var exp = fi.NotIn(Role.FindSQLWithKey());
            var where = exp.GetString(null);
            Assert.Equal("RoleID Not In(Select ID From Role)", where);
        }
    }
}