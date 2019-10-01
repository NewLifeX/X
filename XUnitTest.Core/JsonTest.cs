using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;
using NewLife.Serialization;

namespace XUnitTest.Core
{
    public class JsonTest
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

            var js = role.ToJson(true, false, false);
            //Console.WriteLine(js);
            Assert.True(js.StartsWith("{") && js.EndsWith("}"));

            var role2 = js.ToJsonEntity<Role>();
            //Console.WriteLine("{0} {1} {2}", role2.ID, role2.Name, role2.Ex4);

            Assert.Equal(1, role2.ID);
            Assert.Equal("管理员", role2.Name);
            Assert.False(role2.Enable);
            Assert.Equal("All", role2.Ex4);
        }
    }
}
