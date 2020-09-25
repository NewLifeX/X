using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DAL_Mapper_Tests
    {
        [Fact]
        public void Query()
        {
            var dal = User.Meta.Session.Dal;
            var list = dal.Query<MyUser>("select * from user where name=@name", new { Name = "admin" }).ToList();
            Assert.NotNull(list);
            Assert.Single(list);

            var user = list[0];
            Assert.Equal(1, user.Id);
            Assert.Equal("admin", user.Name);
        }

        class MyUser
        {
            public Int32 Id { get; set; }

            public String Name { get; set; }
        }
    }
}