using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;

namespace NewLife.Caching
{
    /// <summary>Redis缓存</summary>
    public class Redis : Cache
    {
        #region 静态
        static Redis()
        {
            ObjectContainer.Current.AutoRegister<Redis, Redis>();
        }

        /// <summary>创建指定服务器的实例</summary>
        /// <param name="server">服务器地址。支持前面加上密码，@分隔</param>
        /// <param name="db">使用的数据库</param>
        /// <returns></returns>
        public static Redis Create(String server, Int32 db)
        {
            if (server.IsNullOrEmpty() || server == ".") server = "127.0.0.1";

            var pass = "";

            // 从后面开始找，密码可能带有@
            var p = server.LastIndexOf('@');
            if (p >= 0)
            {
                pass = server.Substring(0, p);
                server = server.Substring(p + 1);
            }
            //适配多种配置连接字符
            else if (server.Contains(";") && pass.IsNullOrEmpty())
            {
                var dic = server.SplitAsDictionary("=", ";", true);
                pass = dic.ContainsKey("password") ? dic["password"] : "";
                server = dic.ContainsKey("server") ? dic["server"] : "";
            }

            // 借助对象容器，支持外部注入Redis实现
            var rds = ObjectContainer.Current.Resolve<Redis>();
            rds.Server = server;
            rds.Password = pass;
            rds.Db = db;

            return rds;
        }

        /// <summary>创建指定服务器的实例，支持密码</summary>
        /// <param name="server">服务器地址。支持前面加上密码，@分隔</param>
        /// <param name="password">密码</param>
        /// <param name="db">使用的数据库</param>
        /// <returns></returns>
        public static Redis Create(String server, String password, Int32 db)
        {
            if (server.IsNullOrEmpty() || server == ".") server = "127.0.0.1";

            // 借助对象容器，支持外部注入Redis实现
            var rds = ObjectContainer.Current.Resolve<Redis>();
            rds.Server = server;
            rds.Password = password;
            rds.Db = db;

            return rds;
        }
        #endregion

        #region 属性
        /// <summary>服务器</summary>
        public String Server { get; set; }

        /// <summary>密码</summary>
        public String Password { get; set; }

        /// <summary>目标数据库。默认0</summary>
        public Int32 Db { get; set; }

        /// <summary>出错重试次数。如果出现协议解析错误，可以重试的次数，默认3</summary>
        public Int32 Retry { get; set; } = 3;

        /// <summary>完全管道。读取操作是否合并进入管道，默认false</summary>
        public Boolean FullPipeline { get; set; }

        /// <summary>自动管道。管道操作达到一定数量时，自动提交，默认0</summary>
        public Int32 AutoPipeline { get; set; }

        /// <summary>性能计数器</summary>
        public PerfCounter Counter { get; set; }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="config"></param>
        public override void Init(String config)
        {
            if (config.IsNullOrEmpty()) return;

            var dic = config.SplitAsDictionary("=", ";", true);
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

            try
            {
                Commit();
            }
            catch { }

            _Pool.TryDispose();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{Name} Server={Server} Db={Db}";
        #endregion

        #region 子库
        private ConcurrentDictionary<Int32, Redis> _sub = new ConcurrentDictionary<Int32, Redis>();
        /// <summary>为同一服务器创建不同Db的子级库</summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public virtual Redis CreateSub(Int32 db)
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
        class MyPool : ObjectPool<RedisClient>
        {
            public Redis Instance { get; set; }

            protected override RedisClient OnCreate()
            {
                var rds = Instance;
                var svr = rds.Server;
                if (svr.IsNullOrEmpty()) throw new ArgumentNullException(nameof(rds.Server));

                if (!svr.Contains("://")) svr = "tcp://" + svr;

                var uri = new NetUri(svr);
                if (uri.Port == 0) uri.Port = 6379;

                var rc = new RedisClient
                {
                    Server = uri,
                    Password = rds.Password,
                };

                rc.Log = rds.Log;
                if (rds.Db > 0) rc.Select(rds.Db);

                return rc;
            }

            protected override Boolean OnGet(RedisClient value)
            {
                // 借出时清空残留
                value?.Reset();

                return base.OnGet(value);
            }
        }

        private MyPool _Pool;
        /// <summary>连接池</summary>
        public IPool<RedisClient> Pool
        {
            get
            {
                if (_Pool != null) return _Pool;
                lock (this)
                {
                    if (_Pool != null) return _Pool;

                    var pool = new MyPool
                    {
                        Name = Name + "Pool",
                        Instance = this,
                        Min = 2,
                        Max = 1000,
                        IdleTime = 20,
                        AllIdleTime = 120,
                        Log = Log,
                    };

                    return _Pool = pool;
                }
            }
        }

        /// <summary>执行命令</summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="func">回调函数</param>
        /// <param name="write">是否写入操作</param>
        /// <returns></returns>
        public virtual TResult Execute<TResult>(Func<RedisClient, TResult> func, Boolean write = false)
        {
            // 写入或完全管道模式时，才处理管道操作
            if (write || FullPipeline)
            {
                // 管道模式直接执行
                var rds = _client.Value;
                if (rds == null && AutoPipeline > 0) rds = StartPipeline();
                if (rds != null)
                {
                    var rs = func(rds);

                    // 命令数足够，自动提交
                    if (AutoPipeline > 0 && rds.PipelineCommands >= AutoPipeline)
                    {
                        StopPipeline();
                        StartPipeline();
                    }

                    return rs;
                }
            }

            // 统计性能
            var sw = Counter?.StartCount();

            var i = 0;
            do
            {
                // 每次重试都需要重新从池里借出连接
                var client = Pool.Get();
                try
                {
                    client.Reset();
                    var rs = func(client);

                    Counter?.StopCount(sw);

                    return rs;
                }
                catch (InvalidDataException)
                {
                    if (i++ >= Retry) throw;
                }
                finally
                {
                    Pool.Put(client);
                }
            } while (true);
        }

        private readonly ThreadLocal<RedisClient> _client = new ThreadLocal<RedisClient>();
        /// <summary>开始管道模式</summary>
        public virtual RedisClient StartPipeline()
        {
            var rds = _client.Value;
            if (rds == null)
            {
                rds = Pool.Get();
                rds.StartPipeline();

                _client.Value = rds;
            }

            return rds;
        }

        /// <summary>结束管道模式</summary>
        /// <param name="requireResult">要求结果。默认false</param>
        public virtual Object[] StopPipeline(Boolean requireResult = false)
        {
            var rds = _client.Value;
            if (rds == null) return null;
            _client.Value = null;

            // 管道处理不需要重试
            try
            {
                return rds.StopPipeline(requireResult);
            }
            finally
            {
                // 如果不需要结果，则暂停一会，有效清理残留
                if (!requireResult) Thread.Sleep(1);

                rds.Reset();
                Pool.Put(rds);
            }
        }

        /// <summary>提交变更。处理某些残留在管道里的命令</summary>
        /// <returns></returns>
        public override Int32 Commit()
        {
            var rs = StopPipeline();
            if (rs == null) return 0;

            return rs.Length;
        }
        #endregion

        #region 基础操作
        /// <summary>缓存个数</summary>
        public override Int32 Count
        {
            get
            {
                var client = Pool.Get();
                try
                {
                    return client.Execute<Int32>("DBSIZE");
                }
                finally
                {
                    Pool.Put(client);
                }
            }
        }

        /// <summary>所有键</summary>
        public override ICollection<String> Keys
        {
            get
            {
                if (Count > 10000) throw new InvalidOperationException("数量过大时，禁止获取所有键");

                var client = Pool.Get();
                try
                {
                    var rs = client.Execute<String>("KEYS", "*");
                    return rs.Split(Environment.NewLine).ToList();
                }
                finally
                {
                    Pool.Put(client);
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

            if (expire <= 0)
                return Execute(rds => rds.Execute<String>("SET", key, value) == "OK", true);
            else
                return Execute(rds => rds.Execute<String>("SETEX", key, expire, value) == "OK", true);
        }

        /// <summary>获取单体</summary>
        /// <param name="key">键</param>
        public override T Get<T>(String key) => Execute(rds => rds.Execute<T>("GET", key));

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        public override Int32 Remove(params String[] keys) => Execute(rds => rds.Execute<Int32>("DEL", keys), true);

        /// <summary>清空所有缓存项</summary>
        public override void Clear() => Execute(rds => rds.Execute<String>("FLUSHDB"), true);

        /// <summary>是否存在</summary>
        /// <param name="key">键</param>
        public override Boolean ContainsKey(String key) => Execute(rds => rds.Execute<Int32>("EXISTS", key) > 0);

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        public override Boolean SetExpire(String key, TimeSpan expire) => Execute(rds => rds.Execute<String>("EXPIRE", key, (Int32)expire.TotalSeconds) == "1", true);

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
        public override IDictionary<String, T> GetAll<T>(IEnumerable<String> keys) => Execute(rds => rds.GetAll<T>(keys));

        /// <summary>批量设置缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        public override void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1)
        {
            if (values == null || values.Count == 0) return;

            if (expire < 0) expire = Expire;

            // 优化少量读取
            if (values.Count <= 2)
            {
                foreach (var item in values)
                {
                    Set(item.Key, item.Value, expire);
                }
                return;
            }

            Execute(rds => rds.SetAll(values), true);

            // 使用管道批量设置过期时间
            if (expire > 0)
            {
                var ts = TimeSpan.FromSeconds(expire);

                StartPipeline();
                try
                {
                    foreach (var item in values)
                    {
                        SetExpire(item.Key, ts);
                    }
                }
                finally
                {
                    StopPipeline();
                }
            }
        }
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public override Boolean Add<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            if (expire <= 0)
                return Execute(rds => rds.Execute<Int32>("SETNX", key, value) == 1, true);
            else
                return Execute(rds => rds.Execute<Int32>("SETNX", key, value, expire) == 1, true);
        }

        /// <summary>设置新值并获取旧值，原子操作</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public override T Replace<T>(String key, T value) => Execute(rds => rds.Execute<T>("GETSET", key, value), true);

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Increment(String key, Int64 value)
        {
            if (value == 1)
                return Execute(rds => rds.Execute<Int64>("INCR", key), true);
            else
                return Execute(rds => rds.Execute<Int64>("INCRBY", key, value), true);
        }

        /// <summary>累加，原子操作，乘以100后按整数操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Increment(String key, Double value) => Execute(rds => rds.Execute<Double>("INCRBYFLOAT", key, value), true);

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Decrement(String key, Int64 value)
        {
            if (value == 1)
                return Execute(rds => rds.Execute<Int64>("DECR", key), true);
            else
                return Execute(rds => rds.Execute<Int64>("DECRBY", key, value.ToString()), true);
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
        #endregion

        #region 性能测试
        /// <summary>性能测试</summary>
        /// <remarks>
        /// Redis性能测试[随机]，批大小[100]，逻辑处理器 40 个 2,400MHz Intel(R) Xeon(R) CPU E5-2640 v4 @ 2.40GHz
        /// 测试 100,000 项，  1 线程
        /// 赋值 100,000 项，  1 线程，耗时     418ms 速度   239,234 ops
        /// 读取 100,000 项，  1 线程，耗时     520ms 速度   192,307 ops
        /// 删除 100,000 项，  1 线程，耗时     125ms 速度   800,000 ops
        /// 测试 200,000 项，  2 线程
        /// 赋值 200,000 项，  2 线程，耗时     548ms 速度   364,963 ops
        /// 读取 200,000 项，  2 线程，耗时     549ms 速度   364,298 ops
        /// 删除 200,000 项，  2 线程，耗时     315ms 速度   634,920 ops
        /// 测试 400,000 项，  4 线程
        /// 赋值 400,000 项，  4 线程，耗时     694ms 速度   576,368 ops
        /// 读取 400,000 项，  4 线程，耗时     697ms 速度   573,888 ops
        /// 删除 400,000 项，  4 线程，耗时     438ms 速度   913,242 ops
        /// 测试 800,000 项，  8 线程
        /// 赋值 800,000 项，  8 线程，耗时   1,206ms 速度   663,349 ops
        /// 读取 800,000 项，  8 线程，耗时   1,236ms 速度   647,249 ops
        /// 删除 800,000 项，  8 线程，耗时     791ms 速度 1,011,378 ops
        /// 测试 4,000,000 项， 40 线程
        /// 赋值 4,000,000 项， 40 线程，耗时   4,848ms 速度   825,082 ops
        /// 读取 4,000,000 项， 40 线程，耗时   5,399ms 速度   740,877 ops
        /// 删除 4,000,000 项， 40 线程，耗时   6,281ms 速度   636,841 ops
        /// 测试 4,000,000 项， 64 线程
        /// 赋值 4,000,000 项， 64 线程，耗时   6,806ms 速度   587,716 ops
        /// 读取 4,000,000 项， 64 线程，耗时   5,365ms 速度   745,573 ops
        /// 删除 4,000,000 项， 64 线程，耗时   6,716ms 速度   595,592 ops
        /// </remarks>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        public override void Bench(Boolean rand = true, Int32 batch = 100)
        {
            XTrace.WriteLine($"目标服务器：{Server}/{Db}");

            if (AutoPipeline == 0) AutoPipeline = 100;

            base.Bench(rand, batch);
        }

        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        public override void BenchOne(Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            if (rand && batch > 10) times *= 10;

            base.BenchOne(times, threads, rand, batch);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}