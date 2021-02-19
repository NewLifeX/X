using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NewLife.Log;
using NewLife.Serialization;
using XCode;
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
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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

        private IDisposable CreateForBatch(String action)
        {
            var db = "Data\\Membership_Batch.db";
            DAL.AddConnStr("Membership_Batch", $"Data Source={db}", null, "SQLite");

            var dt = Role2.Meta.Table.DataTable.Clone() as IDataTable;
            dt.TableName = $"Role2_{action}";

            // 分表
            var split = Role2.Meta.CreateSplit("Membership_Batch", dt.TableName);

            var session = Role2.Meta.Session;
            session.Dal.SetTables(dt);

            // 清空数据
            session.Truncate();

            return split;
        }

        [Fact]
        public void BatchInsert()
        {
            using var split = CreateForBatch("BatchInsert");

            var list = new List<Role2>
            {
                new Role2 { Name = "管理员" },
                new Role2 { Name = "高级用户" },
                new Role2 { Name = "普通用户" }
            };
            var rs = list.BatchInsert();
            Assert.Equal(list.Count, rs);

            var list2 = Role2.FindAll();
            Assert.Equal(list.Count, list2.Count);
            Assert.Contains(list2, e => e.Name == "管理员");
            Assert.Contains(list2, e => e.Name == "高级用户");
            Assert.Contains(list2, e => e.Name == "普通用户");
        }

        [Fact]
        public void BatchInsertIgnore()
        {
            using var split = CreateForBatch("InsertIgnore");

            var list = new List<Role2>
            {
                new Role2 { Name = "管理员" },
                new Role2 { Name = "高级用户" },
                new Role2 { Name = "普通用户" }
            };
            var rs = list.BatchInsert();
            Assert.Equal(list.Count, rs);

            list = new List<Role2>
            {
                new Role2 { Name = "管理员" },
                new Role2 { Name = "游客" },
            };
            rs = list.BatchInsertIgnore();
            Assert.Equal(1, rs);

            var list2 = Role2.FindAll();
            Assert.Equal(4, list2.Count);
            Assert.Contains(list2, e => e.Name == "管理员");
            Assert.Contains(list2, e => e.Name == "高级用户");
            Assert.Contains(list2, e => e.Name == "普通用户");
            Assert.Contains(list2, e => e.Name == "游客");
        }

        [Fact]
        public void BatchReplace()
        {
            using var split = CreateForBatch("Replace");

            var list = new List<Role2>
            {
                new Role2 { Name = "管理员", Remark="guanliyuan" },
                new Role2 { Name = "高级用户", Remark="gaoji" },
                new Role2 { Name = "普通用户", Remark="putong" }
            };
            var rs = list.BatchInsert();
            Assert.Equal(list.Count, rs);

            var gly = list.FirstOrDefault(e => e.Name == "管理员");
            Assert.NotNull(gly);
            Assert.Equal("guanliyuan", gly.Remark);

            XTrace.WriteLine(Role2.FindAll().ToJson());

            list = new List<Role2>
            {
                new Role2 { Name = "管理员" },
                new Role2 { Name = "游客", Remark="guest" },
            };
            rs = list.BatchReplace();
            // 删除一行，插入2行，但是影响行为2，这一点跟MySql不同
            Assert.Equal(2, rs);

            var list2 = Role2.FindAll();
            XTrace.WriteLine(list2.ToJson());
            Assert.Equal(4, list2.Count);
            Assert.Contains(list2, e => e.Name == "管理员");
            Assert.Contains(list2, e => e.Name == "高级用户");
            Assert.Contains(list2, e => e.Name == "普通用户");
            Assert.Contains(list2, e => e.Name == "游客");

            var gly2 = list2.FirstOrDefault(e => e.Name == "管理员");
            Assert.NotNull(gly2);
            Assert.Null(gly2.Remark);
            // 管理员被删除后重新插入，自增ID改变
            Assert.NotEqual(gly.ID, gly2.ID);
        }
    }
}