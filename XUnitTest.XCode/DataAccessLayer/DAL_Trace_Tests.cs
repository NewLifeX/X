using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using XCode.Membership;
using Xunit;
using NewLife.Serialization;
using XCode.DataAccessLayer;
using System.Linq;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DAL_Trace_Tests
    {
        [Fact]
        public void Test1()
        {
            var tracer = new DefaultTracer { Log = XTrace.Log };
            DAL.GlobalTracer = tracer;

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

            var bs = tracer.TakeAll();
            Assert.NotNull(bs);
            Assert.True(bs.Length >= 3);

            var keys = bs.Select(e => e.Name).ToArray();
            Assert.Contains("db:Membership:Query:Role", keys);
            Assert.Contains("db:Membership:SelectCount:Role", keys);
            Assert.Contains("db:Membership:Insert:Role", keys);
            Assert.Contains("db:Membership:Update:Role", keys);
            Assert.Contains("db:Membership:Delete:Role", keys);
        }
    }
}