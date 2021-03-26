using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class ArrayFieldLikeTests
    {
        private IDatabase _dbUser;
        private IDatabase _dbLog;
        public ArrayFieldLikeTests()
        {
            _dbUser = User.Meta.Session.Dal.Db;
            _dbLog = Log.Meta.Session.Dal.Db;
        }

        [Fact]
        public void Contains()
        {
            var fi = User._.RoleIds;
            var exp = fi.Contains(3);
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleIds Like '%,3,%'", where);

            Assert.Equal("RoleIds Like '%,3,%'", exp);
        }

        [Fact]
        public void ContainsWithParameter()
        {
            var fi = User._.RoleIds;
            var exp = fi.Contains(3);
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("RoleIds Like '%,@RoleIds,%'", where);

            Assert.Equal("RoleIds Like '%,3,%'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal(3, ps["RoleIds"]);
        }
    }
}