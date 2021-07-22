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
            var fi = User._.Name;
            var exp = fi.Contains("dmi");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("Name Like '%dmi%'", where);

            Assert.Equal("Name Like '%dmi%'", exp);
        }

        [Fact]
        public void ContainsWithParameter()
        {
            var fi = User._.Name;
            var exp = fi.Contains("dmi");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("Name Like '%@Name%'", where);

            Assert.Equal("Name Like '%dmi%'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Name"));
            Assert.Equal("dmi", ps["Name"]);
        }

        [Fact]
        public void NotContains()
        {
            var fi = User._.Name;
            var exp = fi.NotContains("dmi");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("Name Not Like '%dmi%'", where);

            Assert.Equal("Name Not Like '%dmi%'", exp);
        }

        [Fact]
        public void NotContainsWithParameter()
        {
            var fi = User._.Name;
            var exp = fi.NotContains("dmi");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("Name Not Like '%@Name%'", where);

            Assert.Equal("Name Not Like '%dmi%'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Name"));
            Assert.Equal("dmi", ps["Name"]);
        }

        [Fact]
        public void StartsWith()
        {
            var fi = User._.Name;
            var exp = fi.StartsWith("dmi");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("Name Like 'dmi%'", where);

            Assert.Equal("Name Like 'dmi%'", exp);
        }

        [Fact]
        public void StartsWithWithParameter()
        {
            var fi = User._.Name;
            var exp = fi.StartsWith("dmi");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("Name Like '@Name%'", where);

            Assert.Equal("Name Like 'dmi%'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Name"));
            Assert.Equal("dmi", ps["Name"]);
        }

        [Fact]
        public void EndsWith()
        {
            var fi = User._.Name;
            var exp = fi.EndsWith("dmi");
            var where = exp.GetString(_dbUser, null);
            Assert.Equal("Name Like '%dmi'", where);

            Assert.Equal("Name Like '%dmi'", exp);
        }

        [Fact]
        public void EndsWithWithParameter()
        {
            var fi = User._.Name;
            var exp = fi.EndsWith("dmi");
            var ps = new Dictionary<String, Object>();
            var where = exp.GetString(_dbUser, ps);
            Assert.Equal("Name Like '%@Name'", where);

            Assert.Equal("Name Like '%dmi'", exp);

            Assert.Single(ps);
            Assert.True(ps.ContainsKey("Name"));
            Assert.Equal("dmi", ps["Name"]);
        }
    }
}