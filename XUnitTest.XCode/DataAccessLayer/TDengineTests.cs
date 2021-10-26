using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using XCode;
using XCode.DataAccessLayer;
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

            {
                // 反向工程
                dal.SetTables(CarMeter.Meta.Table.DataTable);

                var tables = dal.Tables.OrderBy(e => e.Name).ToList();
                Assert.NotNull(tables);
                Assert.True(tables.Count > 0);
                XTrace.WriteLine(tables.ToJson(false, false, false));

                var tb = tables.FirstOrDefault(e => e.TableName == "car_meter");
                Assert.NotNull(tb);
            }

            {
                // 反向工程
                dal.SetTables(PowerMeter.Meta.Table.DataTable);

                var tables = dal.Tables.OrderBy(e => e.Name).ToList();
                Assert.NotNull(tables);
                Assert.True(tables.Count > 0);
                //XTrace.WriteLine(tables.ToJson(false, false, false));

                //var tb = tables.FirstOrDefault(e => e.TableName == "power_meter");
                //Assert.NotNull(tb);
            }
        }

        private IDisposable CreateForBatch(String action)
        {
            var connStr = _ConnStr.Replace("Database=sys;", "Database=Membership_Batch;");
            DAL.AddConnStr("Membership_Batch", connStr, null, "TDengine");

            var dt = CarMeter.Meta.Table.DataTable.Clone() as IDataTable;
            dt.TableName = $"CarMeter_{action}";

            // 分表
            var split = CarMeter.Meta.CreateSplit("Membership_Batch", dt.TableName);

            var session = CarMeter.Meta.Session;
            session.Dal.SetTables(dt);

            return split;
        }

        [Fact]
        public void BatchInsert()
        {
            using var split = CreateForBatch("BatchInsert");

            var now = DateTime.Now;
            var list = new List<CarMeter>
            {
                new CarMeter { Ts = now.AddMilliseconds(111), Speed = 111 },
                new CarMeter { Ts = now.AddMilliseconds(222), Speed = 222 },
                new CarMeter { Ts = now.AddMilliseconds(333), Speed = 333 }
            };
            var rs = list.BatchInsert();
            Assert.Equal(list.Count, rs);

            var list2 = CarMeter.FindAll(null, CarMeter._.Ts.Desc(), null, 0, list.Count);
            Assert.Equal(list.Count, list2.Count);
            Assert.Contains(list2, e => e.Speed == 333);
            Assert.Contains(list2, e => e.Speed == 222);
            Assert.Contains(list2, e => e.Speed == 111);
        }

        [Fact]
        public void PositiveAndNegative()
        {
            var connName = GetType().Name;
            DAL.AddConnStr(connName, _ConnStr, null, "TDengine");
            var dal = DAL.Create(connName);

            var table = DAL.CreateTable();
            table.TableName = $"user_{Rand.Next(1000, 10000)}";
            {
                var col = table.CreateColumn();
                col.ColumnName = "ts";
                col.DataType = typeof(DateTime);
                table.Columns.Add(col);
            }
            {
                var col = table.CreateColumn();
                col.ColumnName = "speed";
                col.DataType = typeof(Int32);
                table.Columns.Add(col);
            }
            {
                var col = table.CreateColumn();
                col.ColumnName = "temp";
                col.DataType = typeof(Single);
                table.Columns.Add(col);
            }

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