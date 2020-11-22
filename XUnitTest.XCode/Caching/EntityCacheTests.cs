using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;
using XCode.Cache;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Caching
{
    public class EntityCacheTests
    {
        static EntityCacheTests()
        {
            CacheBase.Debug = true;
        }

        public EntityCacheTests()
        {
        }

        [Fact]
        public void Test1()
        {
            var cache = new EntityCache<Role>();
            Assert.Equal(0, cache.Times);
            Assert.Equal(10, cache.Expire);
            Assert.False(cache.Using);
            Assert.NotNull(cache.FillListMethod);
            Assert.Equal(cache.FillListMethod, Role.FindAll);
            Assert.True(cache.WaitFirst);
            Assert.Equal(0, cache.Total);
            Assert.Equal(0, cache.Success);

            // 尝试访问
            cache.Expire = 2;
            var list = cache.Entities;

            Assert.Equal(1, cache.Times);
            Assert.True(cache.Using);
            Assert.Equal(1, cache.Total);
            Assert.Equal(0, cache.Success);

            // 再次访问
            var list2 = cache.Entities;

            Assert.Equal(1, cache.Times);
            Assert.Equal(2, cache.Total);
            Assert.Equal(1, cache.Success);

            // 等待超时后，再次访问
            Thread.Sleep(cache.Expire * 1000 + 10);
            var list3 = cache.Entities;

            Assert.Equal(1, cache.Times);
            Assert.Equal(3, cache.Total);
            Assert.Equal(1, cache.Success);
        }

        [Fact]
        public void TestUpdateCacheAsync()
        {
            var cache = new EntityCache<Role>
            {
                Expire = 2
            };

            // 尝试访问
            var list = cache.Entities;
            Assert.Equal(1, cache.Times);

            // 等待超时后，再次访问
            Thread.Sleep(cache.Expire * 1000 + 10);
            var list3 = cache.Entities;

            Assert.Equal(1, cache.Times);

            // 等待更新完成
            Thread.Sleep(1000);
            Assert.Equal(2, cache.Times);
        }

        [Fact]
        public void TestUpdateCacheSync()
        {
            var cache = new EntityCache<Role>
            {
                Expire = 2
            };

            // 尝试访问
            var list = cache.Entities;
            Assert.Equal(1, cache.Times);

            // 2倍超时后，再次访问走同步更新
            Thread.Sleep(cache.Expire * 1000 * 2 + 10);
            var list3 = cache.Entities;

            Assert.Equal(2, cache.Times);
        }

        [Fact]
        public void TestAddRemove()
        {
            var cache = Role.Meta.Cache;
            var count = cache.Entities.Count;

            var r = new Role { Name = "test" };

            cache.Add(r);
            Assert.Equal(count + 1, cache.Entities.Count);

            cache.Remove(r);
            Assert.Equal(count, cache.Entities.Count);
        }
    }
}