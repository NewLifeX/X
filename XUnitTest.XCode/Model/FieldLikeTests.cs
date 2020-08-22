using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class FieldLikeTests
    {
        [Fact]
        public void Contains()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.Contains(",1,2,3,");
            var where = exp.GetString(UserX.Meta.Session, null);
            Assert.Equal("RoleIds Like '%,1,2,3,%'", where);
        }

        [Fact]
        public void ContainsWithParameter()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.Contains(",1,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(UserX.Meta.Session, ps);
            Assert.Equal("RoleIds Like @RoleIds", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal("%,1,%", ps["RoleIds"]);
        }

        [Fact]
        public void NotContains()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.NotContains(",1,2,3,");
            var where = exp.GetString(UserX.Meta.Session, null);
            Assert.Equal("RoleIds Not Like '%,1,2,3,%'", where);
        }

        [Fact]
        public void NotContainsWithParameter()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.NotContains(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(UserX.Meta.Session, ps);
            Assert.Equal("RoleIds Not Like @RoleIds", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal("%,1,2,3,%", ps["RoleIds"]);
        }

        [Fact]
        public void StartsWith()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.StartsWith(",1,2,3,");
            var where = exp.GetString(UserX.Meta.Session, null);
            Assert.Equal("RoleIds Like ',1,2,3,%'", where);
        }

        [Fact]
        public void StartsWithWithParameter()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.StartsWith(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(UserX.Meta.Session, ps);
            Assert.Equal("RoleIds Like @RoleIds", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal(",1,2,3,%", ps["RoleIds"]);
        }

        [Fact]
        public void EndsWith()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.EndsWith(",1,2,3,");
            var where = exp.GetString(UserX.Meta.Session, null);
            Assert.Equal("RoleIds Like '%,1,2,3,'", where);
        }

        [Fact]
        public void EndsWithWithParameter()
        {
            var fi = UserX._.RoleIds;
            var exp = fi.EndsWith(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(UserX.Meta.Session, ps);
            Assert.Equal("RoleIds Like @RoleIds", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIds"));
            Assert.Equal("%,1,2,3,", ps["RoleIds"]);
        }
    }
}