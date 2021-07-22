using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Log;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class PostgreSQLTests
    {
        private static String _ConnStr = "Server=.;Database=postgres;Uid=postgres;Pwd=postgres";

        public PostgreSQLTests()
        {
            var f = "Config\\pgsql.config".GetFullPath();
            if (File.Exists(f))
                _ConnStr = File.ReadAllText(f);
            else
                File.WriteAllText(f, _ConnStr);
        }

        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.PostgreSQL);
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
            var db = DbFactory.Create(DatabaseType.PostgreSQL);
            var factory = db.Factory;

            var conn = factory.CreateConnection();
            //conn.ConnectionString = "Server=localhost;Database=Membership;Uid=postgres;Pwd=Pass@word";
            conn.ConnectionString = _ConnStr.Replace("Server=.;", "Server=localhost;");
            conn.Open();
        }

        [Fact]
        public void DALTest()
        {
            DAL.AddConnStr("sysPgSql", _ConnStr, null, "PostgreSQL");
            var dal = DAL.Create("sysPgSql");
            Assert.NotNull(dal);
            Assert.Equal("sysPgSql", dal.ConnName);
            Assert.Equal(DatabaseType.PostgreSQL, dal.DbType);

            var db = dal.Db;
            var connstr = db.ConnectionString;
            Assert.Equal("postgres", db.DatabaseName);
            Assert.EndsWith(";Database=postgres;Uid=postgres;Pwd=postgres", connstr.Replace("Pass@word", "postgres"));

            var ver = db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void MetaTest()
        {
            var connStr = _ConnStr.Replace("Database=postgres;", "Database=Membership;");
            DAL.AddConnStr("PgSql_Meta", connStr, null, "PostgreSQL");
            var dal = DAL.Create("PgSql_Meta");

            // 反向工程
            dal.SetTables(User.Meta.Table.DataTable);

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);
        }

        [Fact]
        public void SelectTest()
        {
            DAL.AddConnStr("sysPgSql", _ConnStr, null, "PostgreSQL");
            var dal = DAL.Create("sysPgSql");
            try
            {
                dal.Execute("drop database membership_test");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=postgres;", "Database=Membership_Test;");
            DAL.AddConnStr("PgSql_Select", connStr, null, "PostgreSQL");

            Role.Meta.ConnName = "PgSql_Select";
            Area.Meta.ConnName = "PgSql_Select";

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
            try
            {
                dal.Execute("drop database membership_test");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }
        }

        [Fact]
        public void TablePrefixTest()
        {
            DAL.AddConnStr("sysPgSql", _ConnStr, null, "PostgreSQL");
            var dal = DAL.Create("sysPgSql");
            try
            {
                dal.Execute("drop database membership_table_prefix");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=postgres;", "Database=Membership_Table_Prefix;");
            connStr += ";TablePrefix=member_";
            DAL.AddConnStr("PgSql_Table_Prefix", connStr, null, "PostgreSQL");

            Role.Meta.ConnName = "PgSql_Table_Prefix";
            //Area.Meta.ConnName = "PgSql_Table_Prefix";

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
            var connStr = _ConnStr.Replace("Database=postgres;", "Database=Membership_Batch;");
            DAL.AddConnStr("Membership_Batch", connStr, null, "PostgreSQL");

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
    }
}