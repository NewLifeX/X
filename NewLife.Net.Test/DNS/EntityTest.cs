using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Net.DNS;

namespace NewLife.Net.Test.DNS
{
    [TestClass]
    public class EntityTest
    {
        public EntityTest() { }

        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，该上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext { get { return testContextInstance; } set { testContextInstance = value; } }

        [TestMethod]
        public void DNSEntityReadWrite()
        {
            //try
            //{
            var entity = new DNSEntity();
            entity.Name = "www.newlifex.com";
            entity.Type = DNSQueryType.A;

            var ms = entity.GetStream(false);

            var entity2 = DNSEntity.Read(ms, false);

            Assert.IsNotNull(entity2, "无法读取DNSEntity！");

            Assert.AreEqual(entity.Name, entity2.Name, "DNSEntity读取失败！");
            //}
            //catch (Exception ex)
            //{
            //    Assert.Fail(ex.Message);
            //}
        }
    }
}