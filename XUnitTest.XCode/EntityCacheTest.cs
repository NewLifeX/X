using System;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode
{
    public class EntityCacheTest
    {
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
