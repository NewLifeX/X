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
            var fi = UserX._.RoleIDs;
            var exp = fi.Contains(",1,2,3,");
            var where = exp.GetString(null);
            Assert.Equal("RoleIDs Like '%,1,2,3,%'", where);
        }

        [Fact]
        public void ContainsWithParameter()
        {
            var fi = UserX._.RoleIDs;
            var exp = fi.Contains(",1,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(ps);
            Assert.Equal("RoleIDs Like @RoleIDs", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIDs"));
            Assert.Equal("%,1,%", ps["RoleIDs"]);
        }

        [Fact]
        public void NotContains()
        {
            var fi = UserX._.RoleIDs;
            var exp = fi.NotContains(",1,2,3,");
            var where = exp.GetString(null);
            Assert.Equal("RoleIDs Not Like '%,1,2,3,%'", where);
        }

        [Fact]
        public void NotContainsWithParameter()
        {
            var fi = UserX._.RoleIDs;
            var exp = fi.NotContains(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(ps);
            Assert.Equal("RoleIDs Not Like @RoleIDs", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIDs"));
            Assert.Equal("%,1,2,3,%", ps["RoleIDs"]);
        }

        [Fact]
        public void StartsWith()
        {
            var fi = UserX._.RoleIDs;
            var exp = fi.StartsWith(",1,2,3,");
            var where = exp.GetString(null);
            Assert.Equal("RoleIDs Like ',1,2,3,%'", where);
        }

        [Fact]
        public void StartsWithWithParameter()
        {
            var fi = UserX._.RoleIDs;
            var exp = fi.StartsWith(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(ps);
            Assert.Equal("RoleIDs Like @RoleIDs", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIDs"));
            Assert.Equal(",1,2,3,%", ps["RoleIDs"]);
        }

        [Fact]
        public void EndsWith()
        {
            var fi = UserX._.RoleIDs;
            var exp = fi.EndsWith(",1,2,3,");
            var where = exp.GetString(null);
            Assert.Equal("RoleIDs Like '%,1,2,3,'", where);
        }

        [Fact]
        public void EndsWithWithParameter()
        {
            var fi = UserX._.RoleIDs;
            var exp = fi.EndsWith(",1,2,3,");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(ps);
            Assert.Equal("RoleIDs Like @RoleIDs", where);
            Assert.Single(ps);
            Assert.True(ps.ContainsKey("RoleIDs"));
            Assert.Equal("%,1,2,3,", ps["RoleIDs"]);
        }
    }
}