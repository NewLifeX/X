using System;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Reflection
{
    public class AssemblyXTests
    {
        [Fact]
        public void GetCompileTime()
        {
            {
                var ver = "2.0.8153.37437";
                var time = AssemblyX.GetCompileTime(ver);
                Assert.Equal("2022-04-28 20:47:54".ToDateTime(), time);
            }
            {
                var ver = "9.0.2022.427";
                var time = AssemblyX.GetCompileTime(ver);
                Assert.Equal("2022-04-27 00:00:00".ToDateTime(), time);
            }
            {
                var ver = "9.0.2022.0427-beta0344";
                var time = AssemblyX.GetCompileTime(ver);
                Assert.Equal("2022-04-27 03:44:00".ToDateTime(), time.ToUniversalTime());
            }
        }
    }
}
