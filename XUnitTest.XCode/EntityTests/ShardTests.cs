using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Data;
using NewLife.Security;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Shards;
using Xunit;

namespace XUnitTest.XCode.EntityTests
{
    public class ShardTests
    {
        public ShardTests()
        {
            DAL.AddConnStr("mysql", "Server=.;Port=3306;Database=membership;Uid=root;Pwd=root", null, "mysql");
            DAL.AddConnStr("mysql_underline", "Server=.;Port=3306;Database=membership_underline;Uid=root;Pwd=root;NameFormat=underline", null, "mysql");
        }

        [Fact]
        public void SplitTestSQLite()
        {
            User2.Meta.ShardTableName = e => $"User_{e.RegisterTime:yyyyMM}";

            var user = new User2
            {
                Name = "Stone",
                DisplayName = "大石头",
                Enable = true,

                RegisterTime = new DateTime(2020, 8, 22),
                UpdateTime = new DateTime(2020, 9, 1),
            };
            User2.Meta.CreateShard(user);

            var factory = User2.Meta.Factory;
            var session = User2.Meta.Session;

            var sql = factory.Persistence.GetSql(session, user, DataObjectMethodType.Insert);
            Assert.Equal(@"Insert Into User_202008(Name,Password,DisplayName,Sex,Mail,Mobile,Code,Avatar,RoleID,RoleIds,DepartmentID,Online,Enable,Logins,LastLogin,LastLoginIP,RegisterTime,RegisterIP,Ex1,Ex2,Ex3,Ex4,Ex5,Ex6,UpdateUser,UpdateUserID,UpdateIP,UpdateTime,Remark) Values('Stone',null,'大石头',0,null,null,null,null,0,null,0,0,1,0,null,null,'2020-08-22 00:00:00',null,0,0,0,null,null,null,null,0,null,'2020-09-01 00:00:00',null)", sql);

            user.ID = 2;
            sql = factory.Persistence.GetSql(session, user, DataObjectMethodType.Update);
            Assert.Equal(@"Update User_202008 Set Name='Stone',DisplayName='大石头',Enable=1,RegisterTime='2020-08-22 00:00:00',UpdateTime='2020-09-01 00:00:00' Where ID=2", sql);

            sql = factory.Persistence.GetSql(session, user, DataObjectMethodType.Delete);
            Assert.Equal(@"Delete From User_202008 Where ID=2", sql);

            // 恢复现场，避免影响其它测试用例
            User2.Meta.ShardTableName = null;
        }

        [Fact]
        public void ShardTestSQLite()
        {
            // 配置自动分表策略，一般在实体类静态构造函数中配置
            var shard = new TimeShardPolicy("RegisterTime", User2.Meta.Factory)
            {
                //Field = User2._.RegisterTime,
                TablePolicy = "{0}_{1:yyyyMM}",
            };
            User2.Meta.ShardPolicy = shard;

            // 拦截Sql
            var sql = "";
            DAL.LocalFilter = s => sql = s;

            var user = new User2
            {
                Name = Rand.NextString(8),

                RegisterTime = new DateTime(2020, 8, 22),
                UpdateTime = new DateTime(2020, 9, 1),
            };

            // 添删改查全部使用新表名
            user.Insert();
            Assert.StartsWith(@"[test] Insert Into User2_202008(", sql);

            user.DisplayName = Rand.NextString(16);
            user.Update();
            Assert.StartsWith(@"[test] Update User2_202008 Set", sql);

            user.Delete();
            Assert.StartsWith(@"[test] Delete From User2_202008 Where", sql);

            // 恢复现场，避免影响其它测试用例
            User2.Meta.ShardPolicy = null;
        }

        [Fact]
        public void ShardTestSQLite2()
        {
            // 配置自动分表策略，一般在实体类静态构造函数中配置
            var shard = new TimeShardPolicy("ID", Log2.Meta.Factory)
            {
                //Field = Log2._.ID,
                ConnPolicy = "{0}_{1:yyyy}",
                TablePolicy = "{0}_{1:yyyyMMdd}",
            };
            Log2.Meta.ShardPolicy = shard;

            // 拦截Sql，仅为了断言，非业务代码
            var sqls = new List<String>();
            DAL.LocalFilter = s => sqls.Add(s);

            var time = DateTime.Now;
            var log = new Log2
            {
                Action = "分表",
                Category = Rand.NextString(8),

                CreateTime = time,
            };

            // 添删改查全部使用新表名
            log.Insert();
            Assert.StartsWith($"[test_{time:yyyy}] Insert Into Log2_{time:yyyyMMdd}(", sqls[^1]);

            log.Category = Rand.NextString(16);
            log.Update();
            Assert.StartsWith($"[test_{time:yyyy}] Update Log2_{time:yyyyMMdd} Set", sqls[^1]);

            log.Delete();
            Assert.StartsWith($"[test_{time:yyyy}] Delete From Log2_{time:yyyyMMdd} Where", sqls[^1]);

            var list = Log2.Search(null, null, -1, null, -1, time.AddHours(-24), time, null, new PageParameter { PageSize = 100 });
            Assert.StartsWith($"[test_{time:yyyy}] Select * From Log2_{time.AddHours(-24):yyyyMMdd} Where", sqls[^2]);
            Assert.StartsWith($"[test_{time:yyyy}] Select * From Log2_{time:yyyyMMdd} Where", sqls[^1]);

            // 恢复现场，避免影响其它测试用例
            Log2.Meta.ShardPolicy = null;
        }

        [Fact]
        public void FindById()
        {
            // 配置自动分表策略，一般在实体类静态构造函数中配置
            var shard = new TimeShardPolicy("ID", Log2.Meta.Factory)
            {
                ConnPolicy = "{0}_{1:yyyy}",
                TablePolicy = "{0}_{1:yyyyMMdd}",
            };
            Log2.Meta.ShardPolicy = shard;

            // 拦截Sql，仅为了断言，非业务代码
            var sqls = new List<String>();
            DAL.LocalFilter = s => sqls.Add(s);

            var time = DateTime.Now;
            var log = new Log2
            {
                Action = "分表",
                Category = Rand.NextString(8),

                CreateTime = time,
            };

            // 添删改查全部使用新表名
            log.Insert();
            Assert.StartsWith($"[test_{time:yyyy}] Insert Into Log2_{time:yyyyMMdd}(", sqls[^1]);

            var log2 = Log2.FindByID(log.ID);
            Assert.StartsWith($"[test_{time:yyyy}] Select * From Log2_{time:yyyyMMdd} Where ID=" + log.ID, sqls[^1]);

            // 恢复现场，避免影响其它测试用例
            Log2.Meta.ShardPolicy = null;
        }
    }
}