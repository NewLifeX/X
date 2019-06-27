using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;
using NewLife.Serialization;
using NewLife.Xml;

namespace XUnitTest.Core
{
    public class XmlTest
    {
        [Fact(DisplayName = "基础测试")]
        public void Test1()
        {
            var role = new Role
            {
                ID = 1,
                Name = "管理员",
                Enable = false,
                Ex4 = "All"
            };

            var xml = role.ToXml();
            Assert.Contains("<Role>", xml);
            Assert.Contains("</Role>", xml);

            var xml2 = role.ToXml(null, false, true);
            Assert.Contains("<Role ", xml2);

            var role2 = xml.ToXmlEntity<Role>();

            Assert.Equal(1, role2.ID);
            Assert.Equal("管理员", role2.Name);
            Assert.False(role2.Enable);
            Assert.Equal("All", role2.Ex4);
        }
    }
}