using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
using NewLife.Serialization;
using NewLife.Security;

namespace XUnitTest.XCode.EntityTests
{
    /// <summary>读写分离测试</summary>
    public class ReadWriteTests
    {
        static ReadWriteTests()
        {
#if DEBUG
            NewLife.Setting.Current.LogLevel = LogLevel.All;
#endif
        }

        [Fact]
        public void RWTest()
        {
            // 准备连接字符串。估计放到不同库上
            DAL.AddConnStr("rw_test", "Data Source=data\\rw_test.db", null, "SQLite");
            DAL.AddConnStr("rw_test.readonly", "Data Source=data\\rw_test_readonly.db", null, "SQLite");

            // 反向工程建表
            var d1 = DAL.Create("rw_test");
            var d2 = DAL.Create("rw_test.readonly");
            Role2.Meta.ConnName = "rw_test";
            d1.SetTables(Role2.Meta.Table.DataTable);
            d2.SetTables(Role2.Meta.Table.DataTable);

            var n1 = Role2.Meta.Count;

            var name = Rand.NextString(8);
            XTrace.WriteLine("开始RWTest name={0}", name);

            // 先删掉原来可能有的
            var r0 = Role2.FindByName(name);
            r0?.Delete();

            // 主库插入数据
            XTrace.WriteLine("主库插入数据");
            var r = new Role2();
            r.Name = name;
            r.Insert();

            // 如果查询落在从库，不可能查到。因为这个测试用例故意分开为不同的库
            XTrace.WriteLine("从库查一下");
            var r2 = Role2.FindByName(name);
            //XTrace.WriteLine("FindByName: {0}", r2.ToJson());
            Assert.Null(r2);

            // 更新数据，还在主库
            XTrace.WriteLine("再次更新主库");
            r.IsSystem = true;
            r.Update();

            // 再找一次，理论上还是没有
            XTrace.WriteLine("再找一次从库");
            var r3 = Role2.Find(Role2._.Name == name);
            //XTrace.WriteLine("Find: {0}", r3.ToJson());
            Assert.Null(r3);

            XTrace.WriteLine("删除数据");
            r.Delete();

            XTrace.WriteLine("查从库记录数");
            var n = Role2.FindCount();
            XTrace.WriteLine("count={0}", n);

        }
    }
}