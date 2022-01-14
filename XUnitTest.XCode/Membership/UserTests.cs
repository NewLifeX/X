using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Security;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Membership
{
    public class UserTests
    {
        [Fact]
        public void TestRoleIds()
        {
            var user = new User
            {
                Name = Rand.NextString(16),
                RoleIds = ",3,2,1,7,4",
            };
            user.Insert();

            Assert.Equal(1, user.RoleID);
            Assert.Equal(4, user.RoleIds.SplitAsInt().Length);
            Assert.Equal(",2,3,4,7,", user.RoleIds);

            var user2 = User.FindByKey(user.ID);
            Assert.Equal(1, user2.RoleID);
            Assert.Equal(4, user2.RoleIds.SplitAsInt().Length);
            Assert.Equal(",2,3,4,7,", user2.RoleIds);

            user2.RoleIds = "5,3,9,2,";
            user2.Update();

            var user3 = User.FindByKey(user.ID);
            Assert.Equal(1, user3.RoleID);
            Assert.Equal(4, user3.RoleIds.SplitAsInt().Length);
            Assert.Equal(",2,3,5,9,", user3.RoleIds);

            var dal = User.Meta.Session.Dal;
            var str = dal.QuerySingle<String>("select roleIds from user where id=@id", new { id = user.ID });
            Assert.Equal(",2,3,5,9,", str);

            //var ids = dal.QuerySingle<Int32[]>("select roleIds from user where id=@id", new { id = user.ID });
            //Assert.Equal(new[] { 2, 3, 5, 9 }, ids);
        }

        [Fact]
        public void StringLength()
        {
            var user = new User { Name = Rand.NextString(64) };
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => user.Insert());
            Assert.Equal("Name", ex.ParamName);
            Assert.Equal("名称长度限制50字符 (Parameter 'Name')", ex.Message);
        }
    }
}