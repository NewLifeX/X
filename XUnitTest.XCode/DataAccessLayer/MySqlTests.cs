using System;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class MySqlTests
    {
        private static String _ConnStr = "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word";

        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.MySql);
            Assert.NotNull(db);

            var factory = db.Factory;
            Assert.NotNull(factory);

            var conn = factory.CreateConnection();
            Assert.NotNull(conn);

            var cmd = factory.CreateCommand();
            Assert.NotNull(cmd);

            var adp = factory.CreateDataAdapter();
            Assert.NotNull(adp);

            var dp = factory.CreateParameter();
            Assert.NotNull(dp);
        }

        [Fact]
        public void ConnectTest()
        {
            var db = DbFactory.Create(DatabaseType.MySql);
            var factory = db.Factory;

            var conn = factory.CreateConnection();
            conn.ConnectionString = "Server=localhost;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word";
            conn.Open();
        }

        [Fact]
        public void DALTest()
        {
            DAL.AddConnStr("MySql", _ConnStr, null, "MySql");
            var dal = DAL.Create("MySql");
            Assert.NotNull(dal);
            Assert.Equal("MySql", dal.ConnName);
            Assert.Equal(DatabaseType.MySql, dal.DbType);

            var db = dal.Db;
            var connstr = db.ConnectionString;
            Assert.Equal("Membership", db.DatabaseName);
            Assert.Equal("Server=127.0.0.1;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word;CharSet=utf8mb4;Sslmode=none", connstr);

            var ver = db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void MetaTest()
        {
            DAL.AddConnStr("MySql", _ConnStr, null, "MySql");
            var dal = DAL.Create("MySql");

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);
        }

        [Fact]
        public void SelectTest()
        {
            DAL.AddConnStr("MySql", _ConnStr, null, "MySql");

            Role.Meta.ConnName = "MySql";
            Area.Meta.ConnName = "MySql";

            Role.Meta.Session.InitData();

            var count = Role.Meta.Count;
            Assert.True(count > 0);

            var list = Role.FindAll();
            Assert.Equal(4, list.Count);

            var list2 = Role.FindAll(Role._.Name == "管理员");
            Assert.Equal(1, list2.Count);

            var list3 = Role.Search("用户", null);
            Assert.Equal(2, list3.Count);

            // 来个耗时操作，把前面堵住
            Area.FetchAndSave();
        }
    }
}