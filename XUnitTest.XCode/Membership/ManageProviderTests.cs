using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Membership
{
    public class ManageProviderTests
    {
        [Fact]
        public void UserNull()
        {
            var user = ManageProvider.User;
            Assert.Null(user);
        }
    }
}
