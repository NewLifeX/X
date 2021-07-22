using System;
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
        }

        [Fact]
        public void Test1()
        {
            var cache = new SingleEntityCache<Int32, User>();
            Assert.Equal(10, cache.Expire);
            Assert.Equal(60, cache.ClearPeriod);
            Assert.Equal(10000, cache.MaxEntity);
            Assert.False(cache.Using);
            Assert.NotNull(cache.GetKeyMethod);
            Assert.NotNull(cache.FindKeyMethod);
            Assert.Equal(0, cache.Total);
            Assert.Equal(0, cache.Success);
        }

        [Fact]
        public void TestKey()
        {
            var cache = new SingleEntityCache<Int32, User> { Expire = 1 };

            // 首次访问
            var user = cache[1];
            Assert.Equal(0, cache.Success);

            // 再次访问
            var user2 = cache[1];
            Assert.Equal(1, cache.Success);

            Thread.Sleep(cache.Expire * 1000 + 10);

            // 再次访问
            var user3 = cache[1];
            Assert.Equal(2, cache.Success);
        }

        [Fact]
        public void TestSlave()
        {
            var cache = new SingleEntityCache<Int32, User> { Expire = 1 };
            cache.FindSlaveKeyMethod = k => User.Find(User._.Name == k);
            cache.GetSlaveKeyMethod = e => e.Name;

            // 首次访问
            var user = cache.GetItemWithSlaveKey("admin");
            Assert.Equal(0, cache.Success);

            // 再次访问
            var user2 = cache.GetItemWithSlaveKey("admin");
            Assert.Equal(1, cache.Success);

            Thread.Sleep(cache.Expire * 1000 + 10);

            // 再次访问
            var user3 = cache.GetItemWithSlaveKey("admin");
            Assert.Equal(2, cache.Success);
        }
    }
}