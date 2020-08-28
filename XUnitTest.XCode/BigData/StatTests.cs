using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;
using static XCode.Membership.User;

namespace XUnitTest.XCode.BigData
{
    public class StatTests
    {
        [Fact(DisplayName = "聚合")]
        public void AggregateTest()
        {
            //var list = User.FindAll(null, null, User._.Logins.Sum());
        }
    }
}