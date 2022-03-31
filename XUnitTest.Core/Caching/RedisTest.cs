using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NewLife;
using NewLife.Caching;
using NewLife.Configuration;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest.Caching
{
    [TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
    public class RedisTest
    {
        private Redis _redis;

        public RedisTest()
        {
            var config = "";
            var file = @"config\redis.config";
            if (File.Exists(file)) config = File.ReadAllText(file.GetFullPath())?.Trim();
            if (config.IsNullOrEmpty()) config = "server=127.0.0.1:6379;db=3";
            file.GetFullPath().EnsureDirectory(true);
            File.WriteAllText(file.GetFullPath(), config);

            //Redis = Redis.Create("127.0.0.1:6379", "newlife", 4);
            //Redis = Redis.Create("127.0.0.1:6379", null, 4);
            //Redis = new Redis("127.0.0.1:6379", null, 4);

            _redis = new Redis();
            _redis.Init(config);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [TestOrder(0)]
        [Fact]
        public void ConfigTest()
        {
            var str = "127.0.0.1:6379,password=test,syncTimeout=5000,responseTimeout=5000,ResolveDns=true,connectTimeout=10000,keepAlive=5";
            var redis = new Redis();
            redis.Init(str);

            Assert.Equal("127.0.0.1:6379", redis.Server);
            Assert.Equal("test", redis.Password);
            Assert.Equal(5000, redis.Timeout);

            str = "server=127.0.0.1:6379,127.0.0.1:7000;password=test;db=9;" +
                "timeout=5000;MaxMessageSize=1024000;Expire=3600";
            redis = new Redis();
            redis.Init(str);

            Assert.Equal("127.0.0.1:6379,127.0.0.1:7000", redis.Server);
            Assert.Equal("test", redis.Password);
            Assert.Equal(5000, redis.Timeout);
            Assert.Equal(9, redis.Db);
            Assert.Equal(1024000, redis.MaxMessageSize);
            Assert.Equal(3600, redis.Expire);
        }

        [TestOrder(2)]
        [Fact]
        public void ConfigTest2()
        {
            var prv = new HttpConfigProvider
            {
                Server = "http://star.newlifex.com:6600",
                AppId = "Test"
            };

            var rds = new Redis();
            rds.Init(prv["redis6"]);
            Assert.Equal(6, rds.Db);
        }

        [TestOrder(4)]
        [Fact(DisplayName = "基础测试")]
        public void BasicTest()
        {
            var ic = _redis;
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
            Assert.True(ts.TotalSeconds is > 0 and < 2, "过期时间 " + ts);

            var rs = ic.Remove(key2);
            if (ic.AutoPipeline > 0) rs = (Int32)ic.StopPipeline(true)[0];
            Assert.Equal(1, rs);

            Assert.False(ic.ContainsKey(key2));

            ic.Clear();
            ic.StopPipeline(true);
            Assert.True(ic.Count == 0);
        }

        [TestOrder(6)]
        [Fact(DisplayName = "集合测试")]
        public void DictionaryTest()
        {
            var ic = _redis;

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

        [TestOrder(8)]
        [Fact(DisplayName = "高级添加")]
        public void AddReplace()
        {
            var ic = _redis;
            var key = "Name";

            XTrace.WriteLine("redis_version:{0}", ic.Info["redis_version"]);

            ic.Remove(key);
            ic.Set(key, Environment.UserName, 23);
            var rs = ic.Add(key, Environment.MachineName);
            Assert.False(rs);

            ic.Remove(key);
            ic.Set(key, Environment.UserName, 23);
            rs = ic.Add(key, Environment.MachineName, 30);
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

        [TestOrder(10)]
        [Fact]
        public void TryGet()
        {
            var ic = _redis;
            var key = "tcUser";

            var user = new User { Name = "Stone" };

            ic.Set(key, user, 1);
            var v1 = ic.Get<User>(key);
            Assert.NotNull(v1);

            var rs1 = ic.TryGetValue<User>(key, out var v2);
            Assert.True(rs1);
            Assert.NotEqual(v1, v2);
            Assert.Equal(v1.Name, v2.Name);

            // 等过期，再试
            XTrace.WriteLine("等过期，再试");
            Thread.Sleep(1100);

            var v3 = ic.Get<User>(key);
            Assert.Null(v3);

            var rs4 = ic.TryGetValue<User>(key, out var v4);
            Assert.False(rs4);
            Assert.Null(v4);

            // 写入一个无效字符串
            XTrace.WriteLine("写入一个无效字符串");
            ic.Set(key, "xxx", 3);

            var v5 = ic.Get<User>(key);
            Assert.Null(v5);

            // 实际有值，但解码失败
            var rs6 = ic.TryGetValue<User>(key, out var v6);
            Assert.True(rs6);
            Assert.Null(v6);
        }

        [TestOrder(12)]
        [Fact(DisplayName = "累加累减")]
        public void IncDec()
        {
            var ic = _redis;
            var key = "CostInt";
            var key2 = "CostDouble";

            ic.Set(key, 123);
            ic.Increment(key, 22);
            Assert.Equal(123 + 22, ic.Get<Int32>(key));

            ic.Set(key2, 45.6d);
            ic.Increment(key2, 2.2d);
            Assert.True(Math.Round((45.6d + 2.2d) - ic.Get<Double>(key2), 4) < 0.0001);
        }

        [TestOrder(14)]
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

            var ic = _redis;
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

        [TestOrder(20)]
        [Fact(DisplayName = "字节数组")]
        public void TestBuffer()
        {
            var ic = _redis;
            var key = "buf";

            var str = "学无先后达者为师";
            var buf = str.GetBytes();

            ic.Set(key, buf);
            var buf2 = ic.Get<Byte[]>(key);

            Assert.Equal(buf.ToHex(), buf2.ToHex());
        }

        [TestOrder(30)]
        [Fact(DisplayName = "数据包")]
        public void TestPacket()
        {
            var ic = _redis;
            var key = "buf";

            var str = "学无先后达者为师";
            var pk = new Packet(str.GetBytes());

            ic.Set(key, pk);
            var pk2 = ic.Get<Packet>(key);

            Assert.Equal(pk.ToHex(), pk2.ToHex());
        }

        [TestOrder(40)]
        [Fact(DisplayName = "管道")]
        public void TestPipeline()
        {
            var ap = _redis.AutoPipeline;
            _redis.AutoPipeline = 100;

            BasicTest();

            _redis.AutoPipeline = ap;
        }

        [TestOrder(42)]
        [Fact(DisplayName = "管道2")]
        public void TestPipeline2()
        {
            var ap = _redis.AutoPipeline;
            _redis.AutoPipeline = 100;

            var ic = _redis;
            var key = "Name";
            var key2 = "Company";

            ic.Set(key, "大石头");
            ic.Set(key2, "新生命");
            var ss = ic.StopPipeline(true);
            Assert.Equal("OK", ss[0]);
            Assert.Equal("OK", ss[1]);
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
            Assert.True(ts.TotalSeconds is > 0 and < 2, "过期时间");

            var rs = ic.Remove(key2);
            if (ic.AutoPipeline > 0) rs = (Int32)ic.StopPipeline(true)[0];
            Assert.Equal(1, rs);

            Assert.False(ic.ContainsKey(key2));

            ic.Clear();
            ic.StopPipeline(true);
            Assert.True(ic.Count == 0);

            _redis.AutoPipeline = ap;
        }

        [TestOrder(50)]
        [Fact(DisplayName = "正常锁")]
        public void TestLock1()
        {
            var ic = _redis;

            var ck = ic.AcquireLock("lock:TestLock1", 3000);
            var k2 = ck as CacheLock;

            Assert.NotNull(k2);
            Assert.Equal("lock:TestLock1", k2.Key);

            // 实际上存在这个key
            Assert.True(ic.ContainsKey(k2.Key));

            // 取有效期
            var exp = ic.GetExpire(k2.Key);
            Assert.True(exp.TotalMilliseconds <= 3000);

            // 释放锁
            ck.Dispose();

            // 这个key已经不存在
            Assert.False(ic.ContainsKey(k2.Key));
        }

        [TestOrder(52)]
        [Fact(DisplayName = "抢锁失败")]
        public void TestLock2()
        {
            var ic = _redis;

            var ck1 = ic.AcquireLock("lock:TestLock2", 2000);
            // 故意不用using，验证GC是否能回收
            //using var ck1 = ic.AcquireLock("TestLock2", 3000);

            var sw = Stopwatch.StartNew();

            // 抢相同锁，不可能成功。超时时间必须小于3000，否则前面的锁过期后，这里还是可以抢到的
            Assert.Throws<InvalidOperationException>(() => ic.AcquireLock("lock:TestLock2", 1000));

            // 耗时必须超过有效期
            sw.Stop();
            XTrace.WriteLine("TestLock2 ElapsedMilliseconds={0}ms", sw.ElapsedMilliseconds);
            Assert.True(sw.ElapsedMilliseconds >= 1000);

            Thread.Sleep(2000 - 1000 + 100);

            // 那个锁其实已经不在了，缓存应该把它干掉
            Assert.False(ic.ContainsKey("lock:TestLock2"));
        }

        [TestOrder(54)]
        [Fact(DisplayName = "抢锁失败2")]
        public void TestLock22()
        {
            var ic = _redis;

            var ck1 = ic.AcquireLock("lock:TestLock2", 2000);
            // 故意不用using，验证GC是否能回收
            //using var ck1 = ic.AcquireLock("TestLock2", 3000);

            var sw = Stopwatch.StartNew();

            // 抢相同锁，不可能成功。超时时间必须小于3000，否则前面的锁过期后，这里还是可以抢到的
            var ck2 = ic.AcquireLock("lock:TestLock2", 1000, 1000, false);
            Assert.Null(ck2);

            // 耗时必须超过有效期
            sw.Stop();
            XTrace.WriteLine("TestLock2 ElapsedMilliseconds={0}ms", sw.ElapsedMilliseconds);
            Assert.True(sw.ElapsedMilliseconds >= 1000);

            Thread.Sleep(2000 - 1000 + 100);

            // 那个锁其实已经不在了，缓存应该把它干掉
            Assert.False(ic.ContainsKey("lock:TestLock2"));
        }

        [TestOrder(56)]
        [Fact(DisplayName = "抢死锁")]
        public void TestLock3()
        {
            var ic = _redis;

            using var ck = ic.AcquireLock("TestLock3", 1000);

            // 已经过了一点时间
            Thread.Sleep(500);

            // 循环多次后，可以抢到
            using var ck2 = ic.AcquireLock("TestLock3", 1000);
            Assert.NotNull(ck2);
        }

        [TestOrder(60)]
        [Fact(DisplayName = "搜索测试")]
        public void SearchTest()
        {
            var ic = _redis;

            // 添加删除
            ic.Set("username", Environment.UserName, 60);

            //var ss = ic.Search("*");
            var ss = ic.Execute(null, r => r.Execute<String[]>("KEYS", "*"));
            Assert.NotNull(ss);
            Assert.NotEmpty(ss);

            var ss2 = ic.Execute(null, r => r.Execute<String[]>("KEYS", "abcdefg*"));
            Assert.NotNull(ss2);
            Assert.Empty(ss2);

            var n = 0;
            var ss3 = Search(ic, "*", 10, ref n);
            //var ss3 = ic.Execute(null, r => r.Execute<Object[]>("SCAN", n, "MATCH", "*", "COUNT", 10));
            Assert.NotNull(ss3);
            Assert.NotEmpty(ss3);

            var ss4 = Search(ic, "wwee*", 10, ref n);
            //var ss4 = ic.Execute(null, r => r.Execute<Object[]>("SCAN", n, "MATCH", "wwee*", "COUNT", 10));
            Assert.NotNull(ss4);
            Assert.Empty(ss4);
        }

        private String[] Search(Redis rds, String pattern, Int32 count, ref Int32 position)
        {
            var p = position;
            var rs = rds.Execute(null, r => r.Execute<Object[]>("SCAN", p, "MATCH", pattern + "", "COUNT", count));

            if (rs != null)
            {
                position = (rs[0] as Packet).ToStr().ToInt();

                var ps = rs[1] as Object[];
                var ss = ps.Select(e => (e as Packet).ToStr()).ToArray();
                return ss;
            }

            return null;
        }

        [TestOrder(70)]
        [Fact]
        public async void PopAsync()
        {
            var rds = _redis;
            var key = "async_test";

            rds.Remove(key);

            var sw = Stopwatch.StartNew();

            // 异步发送
            var thread = new Thread(s =>
            {
                Thread.Sleep(100);

                rds.Execute(key, r => r.Execute<Int32>("LPUSH", key, "xxx"), true);
            });
            thread.Start();

            var rs = await rds.ExecuteAsync(key, r => r.ExecuteAsync<String[]>("BRPOP", key, 5));

            sw.Stop();

            Assert.NotNull(rs);
            Assert.Equal(2, rs.Length);
            Assert.Equal(key, rs[0]);
            Assert.Equal("xxx", rs[1]);
            //Assert.True(sw.ElapsedMilliseconds >= 100);
        }

        [TestOrder(80)]
        [Fact(DisplayName = "从机测试")]
        public void SlaveTest()
        {
            // 配置两个地址，第一个地址是不可访问的，它会自动切换到第二地址
            var config = "server=127.0.0.1:6000,127.0.0.1:7000,127.0.0.1:6379;db=3;timeout=7000";

            var redis = new Redis();
            redis.Init(config);
#if DEBUG
            redis.Log = XTrace.Log;
#endif

            var ic = redis;
            var key = "Name";
            var key2 = "Company";

            Assert.Equal(7000, ic.Timeout);

            //// 第一次失败
            //var ex = Assert.ThrowsAny<Exception>(() => ic.Count);
            //XTrace.WriteException(ex);

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
            Assert.True(ts.TotalSeconds is > 0 and < 2, "过期时间");

            var rs = ic.Remove(key2);
            if (ic.AutoPipeline > 0) rs = (Int32)ic.StopPipeline(true)[0];
            Assert.Equal(1, rs);

            Assert.False(ic.ContainsKey(key2));

            ic.Clear();
            ic.StopPipeline(true);
            Assert.True(ic.Count == 0);
        }

        [TestOrder(90)]
        [Fact]
        public void AddRedis()
        {
            var ioc = new ObjectContainer();

            var config = new ConfigProvider();
            config["orderRedis"] = "server=127.0.0.1:6379;password=pass;db=7";
            ioc.AddSingleton<IConfigProvider>(config);
            ioc.AddSingleton(provider => new Redis(provider, "orderRedis"));

            var prv = ioc.BuildServiceProvider();

            var rds = prv.GetService<Redis>();

            Assert.Equal("127.0.0.1:6379", rds.Server);
            Assert.Equal("pass", rds.Password);
            Assert.Equal(7, rds.Db);

            // 改变配置数据，影响对象属性
            config["orderRedis"] = "server=10.0.0.1:6379;password=word;db=13";
            config.SaveAll();

            Assert.Equal("10.0.0.1:6379", rds.Server);
            Assert.Equal("word", rds.Password);
            Assert.Equal(13, rds.Db);
        }

        [TestOrder(100)]
        [Fact]
        public void MaxMessageSizeTest()
        {
            var ic = _redis;

            ic.MaxMessageSize = 1028;

            var ex = Assert.Throws<InvalidOperationException>(() => ic.Set("ttt", Rand.NextString(1029)));
            Assert.NotNull(ex);
            Assert.Equal("命令[SET]的数据包大小[1060]超过最大限制[1028]，大key会拖累整个Redis实例，可通过Redis.MaxMessageSize调节。", ex.Message);
        }
    }
}