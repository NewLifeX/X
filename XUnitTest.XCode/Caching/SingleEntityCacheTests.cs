using System.Threading;
using NewLife.Log;
using XCode.Cache;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Caching
{
    public class SingleEntityCacheTests
    {
        static SingleEntityCacheTests()
        {
            CacheBase.Debug = true;
        }

        public SingleEntityCacheTests()
        {
            var count = User.Meta.Count;
        }

        [Fact]
        public void TestKey()
        {
            for (var i = 0; i < 15; i++)
            {
                XTrace.WriteLine("{0}", i);

                var user = User.Meta.SingleCache[1];

                Thread.Sleep(1000);
            }
        }

        [Fact]
        public void TestSlave()
        {
            for (var i = 0; i < 15; i++)
            {
                XTrace.WriteLine("{0}", i);

                var user = User.Meta.SingleCache.GetItemWithSlaveKey("admin");

                Thread.Sleep(1000);
            }
        }
    }
}