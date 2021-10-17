using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Log;
using NewLife.Security;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.TDengine;
using Xunit;
using XUnitTest.XCode.TestEntity;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class TDengineTests
    {
        private static String _ConnStr = "Server=.;Port=6030;Database=db;user=root;password=taosdata";

        public TDengineTests()
        {
            var f = "Config\\td.config".GetFullPath();
            if (File.Exists(f))
                _ConnStr = File.ReadAllText(f);
            else
                File.WriteAllText(f.EnsureDirectory(true), _ConnStr);
        }

        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.TDengine);
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
            var db = DbFactory.Create(DatabaseType.TDengine);
            var factory = db.Factory;

            var conn = factory.CreateConnection() as TDengineConnection;
            Assert.NotNull(conn);

            //conn.ConnectionString = "Server=localhost;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word";
            conn.ConnectionString = _ConnStr.Replace("Server=.;", "Server=localhost;");
            conn.Open();

            Assert.NotEmpty(conn.ServerVersion);
            XTrace.WriteLine("ServerVersion={0}", conn.ServerVersion);
        }

        [Fact]
        public void DALTest()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");
            Assert.NotNull(dal);
            Assert.Equal("sysTDengine", dal.ConnName);
            Assert.Equal(DatabaseType.TDengine, dal.DbType);

            var db = dal.Db;
            var connstr = db.ConnectionString;
            Assert.Equal("db", db.DatabaseName);

            var ver = db.ServerVersion;
            Assert.NotEmpty(ver);
        }

        [Fact]
        public void CreateDatabase()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");

            var rs = dal.Execute("CREATE DATABASE if not exists power KEEP 365 DAYS 10 BLOCKS 6 UPDATE 1;");
            Assert.Equal(0, rs);
        }

        [Fact]
        public void CreateDatabase2()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");

            var rs = dal.Db.CreateMetaData().SetSchema(DDLSchema.CreateDatabase, "newlife");
            Assert.Equal(0, rs);

            rs = dal.Db.CreateMetaData().SetSchema(DDLSchema.DropDatabase, "newlife");
            Assert.Equal(0, rs);
        }

        [Fact]
        public void CreateTable()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");

            var rs = 0;
            rs = dal.Execute("create table if not exists t (ts timestamp, speed int, temp float)");
            Assert.Equal(0, rs);
        }

        [Fact]
        public void CreateSuperTable()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");

            var rs = 0;

            // 创建超级表
            rs = dal.Execute("create stable if not exists meters (ts timestamp, current float, voltage int, phase float) TAGS (location binary(64), groupId int)");
            Assert.Equal(0, rs);

            // 创建表
            var ns = new[] { "北京", "上海", "广州", "深圳", "天津", "重庆", "杭州", "西安", "成都", "武汉" };
            for (var i = 0; i < 10; i++)
            {
                rs = dal.Execute($"create table if not exists m{(i + 1):000} using meters tags('{ns[i]}', {Rand.Next(1, 100)})");
                Assert.Equal(0, rs);
            }
        }

        [Fact]
        public void QueryTest()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");

            var dt = dal.Query("select * from t");
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 20);
            Assert.Equal(3, dt.Columns.Length);
            //Assert.Equal("[{\"ts\":\"2019-07-15 00:00:00\",\"speed\":10},{\"ts\":\"2019-07-15 01:00:00\",\"speed\":20}]", dt.ToJson());
        }

        [Fact]
        public void InsertTest()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");

            var rs = 0;
            //rs = dal.Execute("create table t (ts timestamp, speed int)");
            //Assert.Equal(0, rs);

            var now = DateTime.Now;
            for (var i = 0; i < 10; i++)
            {
                rs = dal.Execute($"insert into t values ('{now.AddMinutes(i).ToFullString()}', {Rand.Next(0, 100)}, {Rand.Next(0, 10000) / 100.0})");
                Assert.Equal(1, rs);
            }
            //for (var i = 0; i < 10; i++)
            //{
            //    rs = dal.Execute($"insert into t values (@time, @speed, @temp)", new
            //    {
            //        time = now.AddMinutes(i),
            //        speed = Rand.Next(0, 100),
            //        temp = Rand.Next(0, 10000) / 100.0,
            //    });
            //    Assert.Equal(1, rs);
            //}
        }

        [Fact]
        public void MetaTest()
        {
            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership;");
            DAL.AddConnStr("TDengine_Meta", connStr, null, "TDengine");
            var dal = DAL.Create("TDengine_Meta");

            // 反向工程
            dal.SetTables(Meter.Meta.Table.DataTable);

            var tables = dal.Tables;
            Assert.NotNull(tables);
            Assert.True(tables.Count > 0);

            var tb = tables.FirstOrDefault(e => e.Name == "Meter");
            Assert.NotNull(tb);
            Assert.NotEmpty(tb.Description);
        }

        [Fact]
        public void SelectTest()
        {
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");
            try
            {
                dal.Execute("drop database membership_test");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Test;");
            DAL.AddConnStr("TDengine_Select", connStr, null, "TDengine");

            Role.Meta.ConnName = "TDengine_Select";
            Area.Meta.ConnName = "TDengine_Select";

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
            DAL.AddConnStr("sysTDengine", _ConnStr, null, "TDengine");
            var dal = DAL.Create("sysTDengine");
            try
            {
                dal.Execute("drop database membership_table_prefix");
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Table_Prefix;");
            connStr += ";TablePrefix=member_";
            DAL.AddConnStr("TDengine_Table_Prefix", connStr, null, "TDengine");

            Role.Meta.ConnName = "TDengine_Table_Prefix";
            //Area.Meta.ConnName = "TDengine_Table_Prefix";

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
            DAL.AddConnStr("Membership_Batch", connStr, null, "TDengine");

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

            list = new List<Role2>
            {
                new Role2 { Name = "管理员" },
                new Role2 { Name = "游客", Remark="guest" },
            };
            rs = list.BatchReplace();
            // 删除一行，插入2行
            Assert.Equal(3, rs);

            var list2 = Role2.FindAll();
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