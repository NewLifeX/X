using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using Xunit;
using NewLife.IO;
using System.IO;
using System.Reflection;
using System.Linq;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DaMengTests
    {
        [Fact]
        public void LoadDllTest()
        {
            var file = "Plugins\\DmProvider.dll".GetFullPath();
            var asm = Assembly.LoadFile(file);
            Assert.NotNull(asm);

            var types = asm.GetTypes();
            var t = types.FirstOrDefault(t => t.Name == "DmClientFactory");
            Assert.NotNull(t);

            var type = asm.GetType("Dm.DmClientFactory");
            Assert.NotNull(type);
        }

        [Fact]
        public void InitTest()
        {
            var db = DbFactory.Create(DatabaseType.DaMeng);
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
    }
}