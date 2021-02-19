using System;
using System.IO;
using System.Linq;
using System.Reflection;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DB2Tests
    {
        private static String _ConnStr = "Database=localhost;Uid=myUsername;Pwd=myPassword;";

        [Fact]
        public void LoadDllTest()
        {
            var file = "Plugins\\IBM.Data.DB2.Core.dll".GetFullPath();
            var asm = Assembly.LoadFile(file);
            Assert.NotNull(asm);

            var types = asm.GetTypes();
            var t = types.FirstOrDefault(t => t.Name == "DB2Factory");
            Assert.NotNull(t);

            var type = asm.GetType("IBM.Data.DB2.Core.DB2Factory");
            Assert.NotNull(type);
        }

        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.DB2);
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
            var db = DbFactory.Create(DatabaseType.DB2);
            var factory = db.Factory;

            var conn = factory.CreateConnection();
            conn.ConnectionString = "Database=localhost;Uid=myUsername;Pwd=myPassword;";
            conn.Open();
        }

        [Fact]
        public void DALTest()
        {
            DAL.AddConnStr("db2", _ConnStr, null, "db2");
            var dal = DAL.Create("db2");
            Assert.NotNull(dal);
            Assert.Equal("db2", dal.ConnName);
            Assert.Equal(DatabaseType.DB2, dal.DbType);

            var db = dal.Db;
            var connstr = db.ConnectionString;
            Assert.Equal("db2role", db.Owner);
            Assert.Equal("Database=localhost;Uid=myUsername;Pwd=myPassword;", connstr);

            var ver = db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void MetaTest()
        {
            DAL.AddConnStr("db2", _ConnStr, null, "db2");
            var dal = DAL.Create("db2");

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);
        }

        [Fact]
        public void SelectTest()
        {
            //DAL.AddConnStr("Membership", _ConnStr, null, "db2");
            DAL.AddConnStr("db2", _ConnStr, null, "db2");

            Role.Meta.ConnName = "db2";
            Area.Meta.ConnName = "db2";

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