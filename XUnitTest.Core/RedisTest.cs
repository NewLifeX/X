using System;
using NewLife.Caching;
using Xunit;

namespace XUnitTest.Core
{
    public class RedisTest
    {
        public Redis Redis { get; set; }

        public RedisTest()
        {
            Redis = Redis.Create("127.0.0.1:6379", 4);
        }

        [Fact(DisplayName = "基础测试")]
        public void Test1()
        {
            var ic = Redis;
            var key = "Name";

            ic.Set(key, "新生命");
            Assert.Equal("新生命", ic.Get<String>(key));

            var count = ic.Count;
            Assert.True(count > 0);

            //var ks = ic.Keys;
        }
    }
}
