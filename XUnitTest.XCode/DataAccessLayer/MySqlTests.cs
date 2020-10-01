using System;
using System.IO;
using NewLife.Log;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class MySqlTests
    {
        private static String _ConnStr = "Server=.;Port=3306;Database=sys;Uid=root;Pwd=root";

        public MySqlTests()
        {
            var f = "Config\\mysql.config".GetFullPath();
            if (File.Exists(f))
                _ConnStr = File.ReadAllText(f);
            else
                File.WriteAllText(f, _ConnStr);
        }

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
            //conn.ConnectionString = "Server=localhost;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word";
            conn.ConnectionString = _ConnStr.Replace("Server=.;", "Server=localhost;");
            conn.Open();
        }

        [Fact]
        public void DALTest()
        {
            DAL.AddConnStr("sysMySql", _ConnStr, null, "MySql");
            var dal = DAL.Create("sysMySql");
            Assert.NotNull(dal);
            Assert.Equal("sysMySql", dal.ConnName);
            Assert.Equal(DatabaseType.MySql, dal.DbType);

            var db = dal.Db;
            var connstr = db.ConnectionString;
            Assert.Equal("sys", db.DatabaseName);
            Assert.EndsWith(";Port=3306;Database=sys;Uid=root;Pwd=root;CharSet=utf8mb4;Sslmode=none;AllowPublicKeyRetrieval=true", connstr.Replace("Pass@word", "root"));

            var ver = db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void MetaTest()
        {
            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership;");
            DAL.AddConnStr("MySql_Meta", connStr, null, "MySql");
            var dal = DAL.Create("MySql_Meta");

            // 反向工程
            dal.SetTables(User.Meta.Table.DataTable);

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);
        }

        [Fact]
        public void SelectTest()
        {
            DAL.AddConnStr("sysMySql", _ConnStr, null, "MySql");
            var dal = DAL.Create("sysMySql");
            try
            {
                dal.Execute("drop database membership_test");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Test;");
            DAL.AddConnStr("MySql_Select", connStr, null, "MySql");

            Role.Meta.ConnName = "MySql_Select";
            Area.Meta.ConnName = "MySql_Select";

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
            DAL.AddConnStr("sysMySql", _ConnStr, null, "MySql");
            var dal = DAL.Create("sysMySql");
            try
            {
                dal.Execute("drop database membership_table_prefix");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Table_Prefix;");
            connStr += ";TablePrefix=member_";
            DAL.AddConnStr("MySql_Table_Prefix", connStr, null, "MySql");

            Role.Meta.ConnName = "MySql_Table_Prefix";
            //Area.Meta.ConnName = "MySql_Table_Prefix";

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
    }
}