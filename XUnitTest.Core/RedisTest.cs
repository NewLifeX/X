using System;
using System.Collections.Generic;
using NewLife.Caching;
using NewLife.Log;
using Xunit;
using NewLife.Serialization;
using NewLife.Data;

namespace XUnitTest.Core
{
    public class RedisTest
    {
        public Redis Redis { get; set; }

        public RedisTest()
        {
            Redis = Redis.Create("127.0.0.1:6379", 4);
            Redis.Log = XTrace.Log;
        }

        [Fact(DisplayName = "基础测试")]
        public void Test1()
        {
            var ic = Redis;
            var key = "Name";
            var key2 = "Company";

            ic.Set(key, "大石头");
            ic.Set(key2, "新生命");
            Assert.Equal("大石头", ic.Get<String>(key));
            Assert.Equal("新生命", ic.Get<String>(key2));

            var count = ic.Count;
            Assert.True(count >= 2);

            // Keys
            var keys = ic.Keys;
            Assert.True(keys.Contains(key));

            // 过期时间
            ic.SetExpire(key, TimeSpan.FromSeconds(1));
            var ts = ic.GetExpire(key);
            Assert.True(ts.TotalSeconds > 0 && ts.TotalSeconds < 2, "过期时间");

            var rs = ic.Remove(key2);
            Assert.Equal(1, rs);

            Assert.False(ic.ContainsKey(key2));

            ic.Clear();
            Assert.True(ic.Count == 0);
        }

        [Fact(DisplayName = "集合测试")]
        public void DictionaryTest()
        {
            var ic = Redis;

            var dic = new Dictionary<String, String>
            {
                ["111"] = "123",
                ["222"] = "abc",
                ["大石头"] = "学无先后达者为师"
            };

            ic.SetAll(dic);
            var dic2 = ic.GetAll<String>(dic.Keys);

            Assert.Equal(dic.Count, dic2.Count);
            foreach (var item in dic)
            {
                Assert.Equal(item.Value, dic2[item.Key]);
            }
        }

        [Fact(DisplayName = "高级添加")]
        public void AddReplace()
        {
            var ic = Redis;
            var key = "Name";

            ic.Set(key, Environment.UserName);
            var rs = ic.Add(key, Environment.MachineName);
            Assert.False(rs);

            var name = ic.Get<String>(key);
            Assert.Equal(Environment.UserName, name);
            Assert.NotEqual(Environment.MachineName, name);

            var old = ic.Replace(key, Environment.MachineName);
            Assert.Equal(Environment.UserName, old);

            name = ic.Get<String>(key);
            Assert.Equal(Environment.MachineName, name);
            Assert.NotEqual(Environment.UserName, name);
        }

        [Fact(DisplayName = "累加累减")]
        public void IncDec()
        {
            var ic = Redis;
            var key = "CostInt";
            var key2 = "CostDouble";

            ic.Set(key, 123);
            ic.Increment(key, 22);
            Assert.Equal(123 + 22, ic.Get<Int32>(key));

            ic.Set(key2, 456d);
            ic.Increment(key2, 22d);
            Assert.Equal(456d + 22d, ic.Get<Double>(key2));
        }

        [Fact(DisplayName = "复杂对象")]
        public void TestObject()
        {
            var obj = new User
            {
                Name = "大石头",
                Company = "NewLife",
                Age = 24,
                Roles = new[] { "管理员", "游客" },
                UpdateTime = DateTime.Now,
            };

            var ic = Redis;
            var key = "user";

            ic.Set(key, obj);
            var obj2 = ic.Get<User>(key);

            Assert.Equal(obj.ToJson(), obj2.ToJson());
        }

        class User
        {
            public String Name { get; set; }
            public String Company { get; set; }
            public Int32 Age { get; set; }
            public String[] Roles { get; set; }
            public DateTime UpdateTime { get; set; }
        }

        [Fact(DisplayName = "字节数组")]
        public void TestBuffer()
        {
            var ic = Redis;
            var key = "buf";

            var str = "学无先后达者为师";
            var buf = str.GetBytes();

            ic.Set(key, buf);
            var buf2 = ic.Get<Byte[]>(key);

            Assert.Equal(buf.ToHex(), buf2.ToHex());
        }

        [Fact(DisplayName = "数据包")]
        public void TestPacket()
        {
            var ic = Redis;
            var key = "buf";

            var str = "学无先后达者为师";
            var pk = new Packet(str.GetBytes());

            ic.Set(key, pk);
            var pk2 = ic.Get<Packet>(key);

            Assert.Equal(pk.ToHex(), pk2.ToHex());
        }
    }
}
