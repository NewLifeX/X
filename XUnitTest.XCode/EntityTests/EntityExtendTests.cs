using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XCode.Membership;
using Xunit;
using NewLife.Serialization;

namespace XUnitTest.XCode.EntityTests
{
    public class EntityExtendTests
    {
        [Fact]
        public void ExtendJson()
        {
            var role = Role.FindAllWithCache().FirstOrDefault();
            role["aaa"] = "bbb";

            var json = role.ToJson(true, true, false);
            Assert.Contains("\"aaa\": \"bbb\"", json);

            var role2 = json.ToJsonEntity<Role>();
            Assert.Equal("bbb", role2["aaa"]);
        }
    }
}