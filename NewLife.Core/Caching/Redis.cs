using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Net;
using NewLife.Security;

namespace NewLife.Caching
{
    /// <summary>Redis缓存</summary>
    public class Redis : Cache
    {
        #region 静态
        /// <summary>创建</summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static Redis Create(String server, Int32 db)
        {
            if (String.IsNullOrEmpty(server) || server == ".") server = "127.0.0.1";

            var pass = "";
            if (server.Contains("@"))
            {
                pass = server.Substring(null, "@");
                server = server.Substring("@", null);
            }

            var name = "{0}_{1}".F(server, db);
            var set = CacheConfig.Current.GetOrAdd(name);
            if (set.Provider.IsNullOrEmpty())
            {
                set.Provider = "Redis";
                set.Value = $"Server={server};Password={pass};Db={db}";
            }

            return Create(set) as Redis;
        }
        #endregion

        #region 属性
        /// <summary>服务器</summary>
        public String Server { get; set; }

        /// <summary>密码</summary>
        public String Password { get; set; }

        /// <summary>目标数据库。默认0</summary>
        public Int32 Db { get; set; }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="set"></param>
        protected override void Init(CacheSetting set)
        {
            var config = set?.Value;
            if (config.IsNullOrEmpty()) return;

            var dic = config.SplitAsDictionary("=", ";");
            if (dic.Count > 0)
            {
                Server = dic["Server"];
                Password = dic["Password"];
                Db = dic["Db"].ToInt();
            }
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _Pool.TryDispose();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return $"{Name} Server={Server} Db={Db}";
        }
        #endregion

        #region 子库
        private ConcurrentDictionary<Int32, Redis> _sub = new ConcurrentDictionary<Int32, Redis>();
        /// <summary>为同一服务器创建不同Db的子级库</summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public Redis CreateSub(Int32 db)
        {
            if (Db != 0) throw new ArgumentOutOfRangeException(nameof(Db), "只有Db=0的库才能创建子级库连接");
            if (db == 0) return this;

            return _sub.GetOrAdd(db, k =>
            {
                var r = new Redis
                {
                    Server = Server,
                    Db = db,
                    Password = Password,
                };
                return r;
            });
        }
        #endregion

        #region 客户端池
        class MyPool : Pool<RedisClient>
        {
            public Redis Instance { get; set; }

            protected override RedisClient Create()
            {
                var rds = Instance;
                var svr = rds.Server;
                if (!svr.Contains("://")) svr = "tcp://" + svr;

                var rc = new RedisClient
                {
                    Server = new NetUri(svr),
                    Password = rds.Password,
                };
                if (rds.Db > 0) rc.Select(rds.Db);

                rc.Log = rds.Log;

                return rc;
            }
        }

        private MyPool _Pool;
        /// <summary>连接池</summary>
        public Pool<RedisClient> Pool
        {
            get
            {
                return _Pool ?? (_Pool = new MyPool
                {
                    Name = "RedisPool",
                    Instance = this,
                    Min = 2,
                    Max = 1000,
                    IdleTime = 20,
                    AllIdleTime = 120,
                    Log = Log,
                });
            }
        }

        /// <summary>执行命令</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual T Execute<T>(Func<RedisClient, T> func)
        {
            using (var pi = Pool.AcquireItem())
            {
                return func(pi.Value);
            }
        }
        #endregion

        #region 基础操作
        /// <summary>缓存个数</summary>
        public override Int32 Count
        {
            get
            {
                using (var pi = Pool.AcquireItem())
                {
                    return (pi.Value.Execute("DBSIZE") as String).ToInt();
                }
            }
        }

        /// <summary>所有键</summary>
        public override ICollection<String> Keys
        {
            get
            {
                if (Count > 10000) throw new InvalidOperationException("数量过大时，禁止获取所有键");

                using (var pi = Pool.AcquireItem())
                {
                    var rs = pi.Value.Execute("KEYS", "*");
                    var list = new List<String>();

                    return list;
                }
            }
        }

        /// <summary>单个实体项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        public override Boolean Set<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            return Execute(rds => rds.Set(key, value, expire));
        }

        /// <summary>获取单体</summary>
        /// <param name="key">键</param>
        public override T Get<T>(String key)
        {
            return Execute(rds => rds.Get<T>(key));
        }

        /// <summary>移除单体</summary>
        /// <param name="key">键</param>
        public override Boolean Remove(String key)
        {
            return Execute(rds => rds.Execute("DEL", key) as String == "1");
        }

        /// <summary>是否存在</summary>
        /// <param name="key">键</param>
        public override Boolean ContainsKey(String key)
        {
            return Execute(rds => rds.Execute("EXISTS", key) as String == "OK");
        }

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        public override Boolean SetExpire(String key, TimeSpan expire)
        {
            return Execute(rds => rds.Execute("EXPIRE", key, ((Int32)expire.TotalSeconds).ToString()) as String == "1");
        }

        /// <summary>获取缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override TimeSpan GetExpire(String key)
        {
            var sec = Execute(rds => rds.Execute<Int32>("TTL", key));
            return TimeSpan.FromSeconds(sec);
        }
        #endregion

        #region 集合操作
        /// <summary>批量获取缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
        {
            return Execute(rds => rds.GetAll<T>(keys));
        }

        /// <summary>批量设置缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        public override void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1)
        {
            if (expire > 0) throw new ArgumentException("批量设置不支持过期时间", nameof(expire));

            Execute(rds => rds.SetAll(values));
        }
        #endregion

        #region 高级操作
        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Increment(String key, Int64 value)
        {
            if (value == 1)
                return Execute(rds => rds.Execute<Int64>("INCR", key));
            else
                return Execute(rds => rds.Execute<Int64>("INCRBY", key, value));
        }

        /// <summary>累加，原子操作，乘以100后按整数操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Increment(String key, Double value)
        {
            return Execute(rds => rds.Execute<Double>("INCRBYFLOAT", key, value));
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Decrement(String key, Int64 value)
        {
            if (value == 1)
                return Execute(rds => rds.Execute<Int64>("DECR", key));
            else
                return Execute(rds => rds.Execute<Int64>("DECRBY", key, value.ToString()));
        }

        /// <summary>递减，原子操作，乘以100后按整数操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Decrement(String key, Double value)
        {
            //return (Double)Decrement(key, (Int64)(value * 100)) / 100;
            return Increment(key, -value);
        }

        /// <summary>添加，不存在时设置</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Add<T>(String key, T value)
        {
            return Execute(rds => rds.Execute<Int32>("SETNX", key, value) == 1);
        }

        /// <summary>设置新值并获取旧值，原子操作</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public T GetSet<T>(String key, T value)
        {
            return Execute(rds => rds.Execute<T>("GETSET", key, value));
        }
        #endregion

        #region 事务
        /// <summary>申请加锁</summary>
        /// <param name="key"></param>
        /// <param name="msTimeout"></param>
        /// <returns></returns>
        public IDisposable AcquireLock(String key, Int32 msTimeout)
        {
            var rlock = new RedisLock(this, key);
            if (!rlock.Acquire(msTimeout)) throw new InvalidOperationException($"锁定[{key}]失败！msTimeout={msTimeout}");

            return rlock;
        }
        #endregion

        #region 性能测试
        /// <summary>性能测试</summary>
        public override void Bench()
        {
            XTrace.WriteLine($"目标服务器：{Server}/{Db}");

            base.Bench();
        }

        /// <summary>测试</summary>
        public static void Test()
        {
            //var rds = new RedisClient
            //{
            //    Log = XTrace.Log,
            //    Server = new NetUri("tcp://127.0.0.1:6379"),
            //};
            var rds = Redis.Create("127.0.0.1:6379", 4);
            rds.Password = "";
            rds.Log = XTrace.Log;
            rds.Pool.Log = XTrace.Log;

            //rds.Bench();
            //return;

            var rc = rds.Pool.Acquire();

            var f = rc.Select(4);
            //Console.WriteLine(f);

            var p = rc.Ping();
            //Console.WriteLine(p);

            var vs = rds.GetAll<String>(new[] { "num", "dd", "dt" });
            Console.WriteLine(vs);

            var num = Rand.Next(10243);
            rds.Set("num", num);
            var num2 = rds.Get<Int16>("num");
            //Console.WriteLine("{0} => {1}", num, num2);

            var d1 = (Double)Rand.Next(10243) / 100;
            rds.Set("dd", d1);
            var d2 = rds.Get<Double>("dd");
            //Console.WriteLine("{0} => {1}", d1, d2);

            var dt = DateTime.Now;
            rds.Set("dt", dt);
            var dt2 = rds.Get<DateTime>("dt");
            //Console.WriteLine("{0} => {1}", dt, dt2);

            var v = Rand.NextString(7);
            rds.Set("name", v);
            v = rds.Get<String>("name");
            //Console.WriteLine(v);

            var buf1 = Rand.NextBytes(35);
            rds.Set("bs", buf1);
            var buf2 = rds.Get<Byte[]>("bs");
            Console.WriteLine(buf1.ToHex());
            Console.WriteLine(buf2.ToHex());

            //var inf = rc.GetInfo();
            //foreach (var item in inf)
            //{
            //    Console.WriteLine("{0}\t{1}", item.Key, item.Value);
            //}

            // 加锁测试
            var sw = Stopwatch.StartNew();
            Parallel.For(0, 5, k =>
            {
                var key = "num";
                using (var rlock = rds.AcquireLock(key, 3000))
                {
                    var vnum = rds.Get<Int32>(key);
                    vnum++;
                    Thread.Sleep(Rand.Next(100, 1000));
                    rds.Set(key, vnum);
                }
            });
            sw.Stop();

            // 加锁结果检查
            var rnum = rds.Get<Int32>("num");
            Console.WriteLine("加锁累加结果：{0} 匹配：{1} 耗时：{2:n0}ms", rnum, rnum == num2 + 5, sw.ElapsedMilliseconds);

            //rds.Quit();
            rds.Dispose();
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}