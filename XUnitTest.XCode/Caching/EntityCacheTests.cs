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
            Thread.Sleep(cache.Expire * 1000 + 100);
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
            Thread.Sleep(cache.Expire * 1000 + 100);
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
            Thread.Sleep(cache.Expire * 1000 * 2 + 100);
            var list3 = cache.Entities;

            Assert.Equal(2, cache.Times);
        }

        [Fact]
        public void TestClear()
        {
            var cache = new EntityCache<Role>
            {
                Expire = 2
            };

            // 尝试访问
            var list = cache.Entities;
            Assert.Equal(1, cache.Times);

            cache.Clear("TestClear", false);

            Thread.Sleep(1000);
            Assert.Equal(1, cache.Times);

            // 再次访问
            var list2 = cache.Entities;

            Assert.Equal(1, cache.Times);
            Thread.Sleep(1000);
            Assert.Equal(2, cache.Times);

            cache.Clear("TestClear", true);

            // 再次访问
            list2 = cache.Entities;

            Assert.Equal(3, cache.Times);

            // 等待更新完成
            Thread.Sleep(1000);
            Assert.Equal(3, cache.Times);
        }

        [Fact]
        public void TestAddRemove()
        {
            var cache = new EntityCache<Role>
            {
                Expire = 2
            };

            var list = cache.Entities;

            var role = new Role { Name = "test" };

            // 添加实体对象
            cache.Add(role);
            var list2 = cache.Entities;
            Assert.NotEqual(list, list2);
            Assert.Equal(list.Count + 1, list2.Count);

            // 删除实体对象，来自内部
            cache.Remove(role);
            var list3 = cache.Entities;
            Assert.True(list != list3);
            Assert.True(list2 != list3);
            Assert.Equal(list.Count, list3.Count);

            // 删除实体对象，来自外部
            cache.Add(role);
            var role2 = new Role { Name = "test" };
            cache.Remove(role2);
            var list4 = cache.Entities;
            Assert.True(list != list4);
            Assert.True(list2 != list4);
            Assert.Equal(list.Count, list4.Count);
        }
    }
}