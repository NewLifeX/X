using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using Xunit;
using NewLife.IO;
using System.IO;
using System.Reflection;
using System.Linq;
using XCode.Membership;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DaMengTests
    {
        private static String _ConnStr = "Server=.;Port=5236;owner=dameng;user=SYSDBA;password=SYSDBA";

        [Fact]
        public void LoadDllTest()
        {
            var file = "Plugins\\DmProvider.dll".GetFullPath();
            var asm = Assembly.LoadFile(file);
            Assert.NotNull(asm);

            var types = asm.GetTypes();
            var t = types.FirstOrDefault(t => t.Name == "DmClientFactory");
            Assert.NotNull(t);

            var type = asm.GetType("Dm.DmClientFactory");
            Assert.NotNull(type);
        }

        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.DaMeng);
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
            var db = DbFactory.Create(DatabaseType.DaMeng);
            var factory = db.Factory;

            var conn = factory.CreateConnection();
            conn.ConnectionString = "Server=localhost;Port=5236;Database=dameng;user=SYSDBA;password=SYSDBA";
            conn.Open();
        }

        [Fact]
        public void DALTest()
        {
            DAL.AddConnStr("DaMeng", _ConnStr, null, "DaMeng");
            var dal = DAL.Create("DaMeng");
            Assert.NotNull(dal);
            Assert.Equal("DaMeng", dal.ConnName);
            Assert.Equal(DatabaseType.DaMeng, dal.DbType);

            var db = dal.Db;
            var connstr = db.ConnectionString;
            Assert.Equal("dameng", db.Owner);
            Assert.Equal("Server=localhost;Port=5236;user=SYSDBA;password=SYSDBA", connstr);

            var ver = db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void MetaTest()
        {
            DAL.AddConnStr("DaMeng", _ConnStr, null, "DaMeng");
            var dal = DAL.Create("DaMeng");

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);
        }

        [Fact]
        public void SelectTest()
        {
            //DAL.AddConnStr("Membership", _ConnStr, null, "DaMeng");
            DAL.AddConnStr("DaMeng", _ConnStr, null, "DaMeng");

            Role.Meta.ConnName = "DaMeng";
            Area.Meta.ConnName = "DaMeng";

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