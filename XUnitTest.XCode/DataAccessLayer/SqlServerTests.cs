﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NewLife;
using NewLife.Log;
using NewLife.Security;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
using XUnitTest.XCode.TestEntity;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class SqlServerTests
    {
        private static String _ConnStr = "Server=127.0.0.1;Database=sys;Uid=root;Pwd=root;Connection Timeout=2";

        public SqlServerTests()
        {
            var f = "Config\\sqlserver.config".GetFullPath();
            if (File.Exists(f))
                _ConnStr = File.ReadAllText(f);
            else
                File.WriteAllText(f.EnsureDirectory(), _ConnStr);
        }

        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.SqlServer);
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
            var db = DbFactory.Create(DatabaseType.SqlServer);
            var factory = db.Factory;

            var conn = factory.CreateConnection();
            //conn.ConnectionString = "Server=localhost;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word";
            conn.ConnectionString = _ConnStr.Replace("Server=.;", "Server=localhost;");
            conn.Open();
        }

        [Fact]
        public void DALTest()
        {
            DAL.AddConnStr("sysSqlServer", _ConnStr, null, "SqlServer");
            var dal = DAL.Create("sysSqlServer");
            Assert.NotNull(dal);
            Assert.Equal("sysSqlServer", dal.ConnName);
            Assert.Equal(DatabaseType.SqlServer, dal.DbType);

            var db = dal.Db;
            var connstr = db.ConnectionString;
            //Assert.Equal("sys", db.DatabaseName);
            Assert.EndsWith(";Application Name=XCode_testhost_sysSqlServer", connstr);

            var ver = db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void MetaTest()
        {
            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership;");
            DAL.AddConnStr("SqlServer_Meta", connStr, null, "SqlServer");
            var dal = DAL.Create("SqlServer_Meta");

            // 反向工程
            dal.SetTables(User.Meta.Table.DataTable);

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);

            var tb = tables.FirstOrDefault(e => e.Name == "User");
            Assert.NotNull(tb);
            Assert.NotEmpty(tb.Description);
        }

        [Fact]
        public void SelectTest()
        {
            DAL.AddConnStr("sysSqlServer", _ConnStr, null, "SqlServer");
            var dal = DAL.Create("sysSqlServer");
            try
            {
                dal.Execute("drop database membership_test");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Test;");
            DAL.AddConnStr("SqlServer_Select", connStr, null, "SqlServer");

            Role.Meta.ConnName = "SqlServer_Select";
            Area.Meta.ConnName = "SqlServer_Select";

            Role.Meta.Session.InitData();

            var count = Role.Meta.Count;
            Assert.True(count > 0);

            var list = Role.FindAll();
            Assert.True(list.Count >= 4);

            var list2 = Role.FindAll(Role._.Name == "管理员");
            Assert.Equal(1, list2.Count);

            var list3 = Role.Search("用户", null);
            Assert.Equal(2, list3.Count);

            // 来个耗时操作，把前面堵住
            Area.FetchAndSave();

            // 清理现场
            try
            {
                dal.Execute("drop database membership_test");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }
        }

        [Fact]
        public void TablePrefixTest()
        {
            DAL.AddConnStr("sysSqlServer", _ConnStr, null, "SqlServer");
            var dal = DAL.Create("sysSqlServer");
            try
            {
                dal.Execute("drop database membership_table_prefix");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Table_Prefix;");
            connStr += ";TablePrefix=member_";
            DAL.AddConnStr("SqlServer_Table_Prefix", connStr, null, "SqlServer");

            Role.Meta.ConnName = "SqlServer_Table_Prefix";
            //Area.Meta.ConnName = "SqlServer_Table_Prefix";

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
            try
            {
                dal.Execute("drop database membership_table_prefix");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }
        }

        private IDisposable CreateForBatch(String action)
        {
            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Batch;");
            DAL.AddConnStr("Membership_Batch", connStr, null, "SqlServer");

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

        //[Fact]
        //public void BatchInsertIgnore()
        //{
        //    using var split = CreateForBatch("InsertIgnore");

        //    var list = new List<Role2>
        //    {
        //        new Role2 { Name = "管理员" },
        //        new Role2 { Name = "高级用户" },
        //        new Role2 { Name = "普通用户" }
        //    };
        //    var rs = list.BatchInsert();
        //    Assert.Equal(list.Count, rs);

        //    list = new List<Role2>
        //    {
        //        new Role2 { Name = "管理员" },
        //        new Role2 { Name = "游客" },
        //    };
        //    rs = list.BatchInsertIgnore();
        //    Assert.Equal(1, rs);

        //    var list2 = Role2.FindAll();
        //    Assert.Equal(4, list2.Count);
        //    Assert.Contains(list2, e => e.Name == "管理员");
        //    Assert.Contains(list2, e => e.Name == "高级用户");
        //    Assert.Contains(list2, e => e.Name == "普通用户");
        //    Assert.Contains(list2, e => e.Name == "游客");
        //}

        //[Fact]
        //public void BatchReplace()
        //{
        //    using var split = CreateForBatch("Replace");

        //    var list = new List<Role2>
        //    {
        //        new Role2 { Name = "管理员", Remark="guanliyuan" },
        //        new Role2 { Name = "高级用户", Remark="gaoji" },
        //        new Role2 { Name = "普通用户", Remark="putong" }
        //    };
        //    var rs = list.BatchInsert();
        //    Assert.Equal(list.Count, rs);

        //    var gly = list.FirstOrDefault(e => e.Name == "管理员");
        //    Assert.NotNull(gly);
        //    Assert.Equal("guanliyuan", gly.Remark);

        //    list = new List<Role2>
        //    {
        //        new Role2 { Name = "管理员" },
        //        new Role2 { Name = "游客", Remark="guest" },
        //    };
        //    rs = list.BatchReplace();
        //    // 删除一行，插入2行
        //    Assert.Equal(3, rs);

        //    var list2 = Role2.FindAll();
        //    Assert.Equal(4, list2.Count);
        //    Assert.Contains(list2, e => e.Name == "管理员");
        //    Assert.Contains(list2, e => e.Name == "高级用户");
        //    Assert.Contains(list2, e => e.Name == "普通用户");
        //    Assert.Contains(list2, e => e.Name == "游客");

        //    var gly2 = list2.FirstOrDefault(e => e.Name == "管理员");
        //    Assert.NotNull(gly2);
        //    Assert.Null(gly2.Remark);
        //    // 管理员被删除后重新插入，自增ID改变
        //    Assert.NotEqual(gly.ID, gly2.ID);
        //}

        [Fact]
        public void PositiveAndNegative()
        {
            var connName = GetType().Name;
            DAL.AddConnStr(connName, _ConnStr, null, "SqlServer");
            var dal = DAL.Create(connName);

            var table = User.Meta.Table.DataTable.Clone() as IDataTable;
            table.TableName = $"user_{Rand.Next(1000, 10000)}";

            dal.SetTables(table);

            var tableNames = dal.GetTableNames();
            XTrace.WriteLine("tableNames: {0}", tableNames.Join());
            Assert.Contains(table.TableName, tableNames);

            var tables = dal.Tables;
            XTrace.WriteLine("tables: {0}", tables.Join());
            Assert.Contains(tables, t => t.TableName == table.TableName);

            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, table);

            tableNames = dal.GetTableNames();
            XTrace.WriteLine("tableNames: {0}", tableNames.Join());
            Assert.DoesNotContain(table.TableName, tableNames);
        }
    }
}