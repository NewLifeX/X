using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class FieldLikeTests
    {
        private IDatabase _dbUser;
        private IDatabase _dbLog;
        public FieldLikeTests()
        {
            _dbUser = User.Meta.Session.Dal.Db;
            _dbLog = Log.Meta.Session.Dal.Db;
        }

        [Fact]
        public void Contains()
        {
            var fi = User._.RoleIds;
            var exp = fi.Contains(",1,2,3,");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleIds Like '%,1,2,3,%'", where);

            Assert.Equal("RoleIds Like '%,1,2,3,%'", exp);
        }

        [Fact]
        public void ContainsWithParameter()
        {
            var fi = User._.RoleIds;
            var exp = fi.Contains(",1,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("RoleIds Like @RoleIds", where);

            Assert.Equal("RoleIds Like '%,1,%'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal("%,1,%", ps["RoleIds"]);
        }

        [Fact]
        public void NotContains()
        {
            var fi = User._.RoleIds;
            var exp = fi.NotContains(",1,2,3,");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleIds Not Like '%,1,2,3,%'", where);

            Assert.Equal("RoleIds Not Like '%,1,2,3,%'", exp);
        }

        [Fact]
        public void NotContainsWithParameter()
        {
            var fi = User._.RoleIds;
            var exp = fi.NotContains(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("RoleIds Not Like @RoleIds", where);

            Assert.Equal("RoleIds Not Like '%,1,2,3,%'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal("%,1,2,3,%", ps["RoleIds"]);
        }

        [Fact]
        public void StartsWith()
        {
            var fi = User._.RoleIds;
            var exp = fi.StartsWith(",1,2,3,");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleIds Like ',1,2,3,%'", where);

            Assert.Equal("RoleIds Like ',1,2,3,%'", exp);
        }

        [Fact]
        public void StartsWithWithParameter()
        {
            var fi = User._.RoleIds;
            var exp = fi.StartsWith(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("RoleIds Like @RoleIds", where);

            Assert.Equal("RoleIds Like ',1,2,3,%'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal(",1,2,3,%", ps["RoleIds"]);
        }

        [Fact]
        public void EndsWith()
        {
            var fi = User._.RoleIds;
            var exp = fi.EndsWith(",1,2,3,");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("RoleIds Like '%,1,2,3,'", where);

            Assert.Equal("RoleIds Like '%,1,2,3,'", exp);
        }

        [Fact]
        public void EndsWithWithParameter()
        {
            var fi = User._.RoleIds;
            var exp = fi.EndsWith(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("RoleIds Like @RoleIds", where);

            Assert.Equal("RoleIds Like '%,1,2,3,'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal("%,1,2,3,", ps["RoleIds"]);
        }
    }
}