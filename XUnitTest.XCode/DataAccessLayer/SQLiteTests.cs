using System;
using System.IO;
using NewLife.Log;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class SQLiteTests
    {
        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.SQLite);
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
            var db = DbFactory.Create(DatabaseType.SQLite);
            var factory = db.Factory;

            var conn = factory.CreateConnection();
            conn.ConnectionString = "Data Source=Data\\Membership.db";
            conn.Open();
        }

        [Fact]
        public void DALTest()
        {
            var db = "Data\\Membership.db";
            var dbf = db.GetFullPath();

            DAL.AddConnStr("sysSQLite", $"Data Source={db}", null, "SQLite");
            var dal = DAL.Create("sysSQLite");
            Assert.NotNull(dal);
            Assert.Equal("sysSQLite", dal.ConnName);
            Assert.Equal(DatabaseType.SQLite, dal.DbType);

            var connstr = dal.Db.ConnectionString;
            Assert.Equal(dbf, dal.Db.DatabaseName);
            Assert.EndsWith("\\Data\\Membership.db;Cache Size=-524288;Synchronous=Off;Journal Mode=WAL", connstr);

            var ver = dal.Db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void MetaTest()
        {
            DAL.AddConnStr("SQLite_Meta", "Data Source=Data\\Membership.db", null, "SQLite");
            var dal = DAL.Create("SQLite_Meta");

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);
        }

        [Fact]
        public void SelectTest()
        {
            var db = "Data\\Membership_Test.db";
            var dbf = db.GetFullPath();
            if (File.Exists(dbf)) File.Delete(dbf);

            DAL.AddConnStr("SQLite_Select", $"Data Source={db}", null, "SQLite");

            Role.Meta.ConnName = "SQLite_Select";
            Area.Meta.ConnName = "SQLite_Select";

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

            // 清理现场
            if (File.Exists(dbf)) File.Delete(dbf);
        }

        [Fact]
        public void TablePrefixTest()
        {
            var db = "Data\\Membership_Table_Prefix.db";
            var dbf = db.GetFullPath();
            if (File.Exists(dbf)) File.Delete(dbf);

            DAL.AddConnStr("SQLite_Table_Prefix", $"Data Source={db}", null, "SQLite");

            Role.Meta.ConnName = "SQLite_Table_Prefix";
            //Area.Meta.ConnName = "SQLite_Table_Prefix";

            Role.Meta.Session.InitData();

            var count = Role.Meta.Count;
            Assert.True(count > 0);

            var list = Role.FindAll();
            Assert.Equal(4, list.Count);

            var list2 = Role.FindAll(Role._.Name == "管理员");
            Assert.Equal(1, list2.Count);

            var list3 = Role.Search("用户", null);
            Assert.Equal(2, list3.Count);

            // 清理现场
            if (File.Exists(dbf)) File.Delete(dbf);
        }
    }
}