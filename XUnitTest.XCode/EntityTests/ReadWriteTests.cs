using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using Xunit;

namespace XUnitTest.XCode.EntityTests
{
    /// <summary>读写分离测试</summary>
    public class ReadWriteTests
    {
        [Fact]
        public void RWTest()
        {
            // 准备连接字符串
            DAL.AddConnStr("Membership", "Data Source=Membership.db", null, "SQLite");
            DAL.AddConnStr("Membership.readonly", "Data Source=Membership.db;ReadOnly=true", null, "SQLite");

            // 先删掉原来可能有的
            var r0 = Role.FindByName("Stone");
            r0?.Delete();

            var r = new Role();
            r.Name = "Stone";
            r.Insert();

            var r2 = Role.FindByName("Stone");
            XTrace.WriteLine("FindByName: {0}", r2.ToJson());

            r.Enable = true;
            r.Update();

            var r3 = Role.Find(Role._.Name == "STONE");
            XTrace.WriteLine("Find: {0}", r3.ToJson());

            r.Delete();

            var n = Role.FindCount();
            XTrace.WriteLine("count={0}", n);

        }
    }
}