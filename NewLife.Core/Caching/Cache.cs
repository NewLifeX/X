using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Security;

namespace NewLife.Caching
{
    /// <summary>缓存</summary>
    public abstract class Cache : DisposeBase, ICache
    {
        #region 静态默认实现
        /// <summary>默认缓存</summary>
        public static ICache Default { get; set; } = new MemoryCache();

        static Cache()
        {
            //// 查找一个外部缓存提供者来作为默认缓存
            //Default = ObjectContainer.Current.AutoRegister<ICache, MemoryCache>().ResolveInstance<ICache>();

            var ioc = ObjectContainer.Current;
            // 遍历所有程序集，自动加载
            foreach (var item in typeof(ICache).GetAllSubclasses(true))
            {
                // 实例化一次，让这个类有机会执行类型构造函数，可以获取旧的类型实现
                if (item.CreateInstance() is ICache ic)
                {
                    var id = ic.Name;
                    if (id.IsNullOrEmpty()) id = item.Name.TrimEnd("Cache");

                    /*if (XTrace.Debug)*/
                    XTrace.WriteLine("发现缓存实现 [{0}] = {1}", id, item.FullName);

                    ioc.Register<ICache>(ic, id);
                }
            }
        }

        private static ConcurrentDictionary<String, ICache> _cache = new ConcurrentDictionary<String, ICache>();
        /// <summary>创建缓存实例</summary>
        /// <param name="set">配置项</param>
        /// <returns></returns>
        public static ICache Create(CacheSetting set)
        {
            return _cache.GetOrAdd(set.Name, k =>
            {
                var id = set.Provider;

                var type = ObjectContainer.Current.ResolveType<ICache>(id);
                if (type == null) throw new ArgumentNullException(nameof(type), "找不到名为[{0}]的缓存实现[{1}]".F(set.Name, id));

                var ic = type.CreateInstance() as ICache;
                if (ic is Cache ic2) ic2.Init(set);

                return ic;
            });
        }

        /// <summary>创建缓存实例</summary>
        /// <param name="name">名字。memory、redis://127.0.0.1:6379?Db=6</param>
        /// <returns></returns>
        public static ICache Create(String name)
        {
            if (name == null) name = "";

            // 尝试直接获取，避免多次调用CacheConfig.GetOrAdd影响应性能
            if (_cache.TryGetValue(name, out var ic)) return ic;

            var item = CacheConfig.Current.GetOrAdd(name);
            return Create(item);
        }
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; protected set; }

        /// <summary>默认缓存时间。默认0秒表示不过期</summary>
        public Int32 Expire { get; set; }

        /// <summary>获取和设置缓存，永不过期</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Object this[String key] { get { return Get<Object>(key); } set { Set(key, value); } }

        /// <summary>缓存个数</summary>
        public abstract Int32 Count { get; }

        /// <summary>所有键</summary>
        public abstract ICollection<String> Keys { get; }
        #endregion

        #region 构造
        /// <summary>构造函数</summary>
        public Cache()
        {
            Name = GetType().Name.TrimEnd("Cache");
        }
        #endregion

        #region 基础操作
        /// <summary>初始化配置</summary>
        /// <param name="set"></param>
        protected virtual void Init(CacheSetting set) { }

        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract Boolean ContainsKey(String key);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Expire"/></param>
        /// <returns></returns>
        public abstract Boolean Set<T>(String key, T value, Int32 expire = -1);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间</param>
        /// <returns></returns>
        public virtual Boolean Set<T>(String key, T value, TimeSpan expire) { return Set(key, value, (Int32)expire.TotalSeconds); }

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public abstract T Get<T>(String key);

        /// <summary>移除缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public abstract Boolean Remove(String key);

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间，秒</param>
        public abstract Boolean SetExpire(String key, TimeSpan expire);

        /// <summary>获取缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public abstract TimeSpan GetExpire(String key);
        #endregion

        #region 集合操作
        /// <summary>批量获取缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
        {
            var dic = new Dictionary<String, T>();
            foreach (var key in keys)
            {
                dic[key] = Get<T>(key);
            }

            return dic;
        }

        /// <summary>批量设置缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Expire"/></param>
        public virtual void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1)
        {
            foreach (var item in values)
            {
                Set(item.Key, item.Value, expire);
            }
        }

        /// <summary>获取列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual IList<T> GetList<T>(String key)
        {
            var list = Get<IList<T>>(key);
            if (list == null)
            {
                list = new List<T>();
                Set(key, list);
            }

            return list;
        }

        /// <summary>获取哈希</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual IDictionary<String, T> GetDictionary<T>(String key)
        {
            var dic = Get<IDictionary<String, T>>(key);
            if (dic == null)
            {
                dic = new Dictionary<String, T>();
                Set(key, dic);
            }

            return dic;
        }
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public virtual Boolean Add<T>(String key, T value, Int32 expire = -1) { return Set(key, value, expire); }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Int64 Increment(String key, Int64 value)
        {
            lock (this)
            {
                var v = Get<Int64>(key);
                v += value;
                Set(key, v);

                return v;
            }
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Double Increment(String key, Double value)
        {
            lock (this)
            {
                var v = Get<Double>(key);
                v += value;
                Set(key, v);

                return v;
            }
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Int64 Decrement(String key, Int64 value)
        {
            lock (this)
            {
                var v = Get<Int64>(key);
                v -= value;
                Set(key, v);

                return v;
            }
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Double Decrement(String key, Double value)
        {
            lock (this)
            {
                var v = Get<Double>(key);
                v -= value;
                Set(key, v);

                return v;
            }
        }
        #endregion

        #region 性能测试
        /// <summary>多线程性能测试</summary>
        /// <param name="rand">随机读写</param>
        /// <remarks>
        /// Memory性能测试[顺序]，逻辑处理器 32 个 2,000MHz Intel(R) Xeon(R) CPU E5-2640 v2 @ 2.00GHz
        /// 
        /// 测试 10,000,000 项，  1 线程
        /// 赋值 10,000,000 项，  1 线程，耗时   3,764ms 速度 2,656,748 ops
        /// 读取 10,000,000 项，  1 线程，耗时   1,296ms 速度 7,716,049 ops
        /// 删除 10,000,000 项，  1 线程，耗时   1,230ms 速度 8,130,081 ops
        /// 
        /// 测试 20,000,000 项，  2 线程
        /// 赋值 20,000,000 项，  2 线程，耗时   3,088ms 速度 6,476,683 ops
        /// 读取 20,000,000 项，  2 线程，耗时   1,051ms 速度 19,029,495 ops
        /// 删除 20,000,000 项，  2 线程，耗时   1,011ms 速度 19,782,393 ops
        /// 
        /// 测试 40,000,000 项，  4 线程
        /// 赋值 40,000,000 项，  4 线程，耗时   3,060ms 速度 13,071,895 ops
        /// 读取 40,000,000 项，  4 线程，耗时   1,023ms 速度 39,100,684 ops
        /// 删除 40,000,000 项，  4 线程，耗时     994ms 速度 40,241,448 ops
        /// 
        /// 测试 80,000,000 项，  8 线程
        /// 赋值 80,000,000 项，  8 线程，耗时   3,124ms 速度 25,608,194 ops
        /// 读取 80,000,000 项，  8 线程，耗时   1,171ms 速度 68,317,677 ops
        /// 删除 80,000,000 项，  8 线程，耗时   1,199ms 速度 66,722,268 ops
        /// 
        /// 测试 320,000,000 项， 32 线程
        /// 赋值 320,000,000 项， 32 线程，耗时  13,857ms 速度 23,093,021 ops
        /// 读取 320,000,000 项， 32 线程，耗时   1,950ms 速度 164,102,564 ops
        /// 删除 320,000,000 项， 32 线程，耗时   3,359ms 速度 95,266,448 ops
        /// 
        /// 测试 320,000,000 项， 64 线程
        /// 赋值 320,000,000 项， 64 线程，耗时   9,648ms 速度 33,167,495 ops
        /// 读取 320,000,000 项， 64 线程，耗时   1,974ms 速度 162,107,396 ops
        /// 删除 320,000,000 项， 64 线程，耗时   1,907ms 速度 167,802,831 ops
        /// 
        /// 测试 320,000,000 项，256 线程
        /// 赋值 320,000,000 项，256 线程，耗时  12,429ms 速度 25,746,238 ops
        /// 读取 320,000,000 项，256 线程，耗时   1,907ms 速度 167,802,831 ops
        /// 删除 320,000,000 项，256 线程，耗时   2,350ms 速度 136,170,212 ops
        /// </remarks>
        public virtual void Bench(Boolean rand = false)
        {
            var processor = "";
            var frequency = 0;
            using (var reg = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
            {
                processor = (reg.GetValue("ProcessorNameString") + "").Trim();
                frequency = reg.GetValue("~MHz").ToInt();
            }

            var cpu = Environment.ProcessorCount;
            XTrace.WriteLine($"{Name}性能测试[{(rand ? "随机" : "顺序")}]，逻辑处理器 {cpu:n0} 个 {frequency:n0}MHz {processor}");

            var times = 10_000;

            // 单线程
            BenchOne(times, 1, rand);

            // 多线程
            if (cpu != 2) BenchOne(times * 2, 2, rand);
            if (cpu != 4) BenchOne(times * 4, 4, rand);
            if (cpu != 8) BenchOne(times * 8, 8, rand);

            // CPU个数
            BenchOne(times * cpu, cpu, rand);

            //// 1.5倍
            //var cpu2 = cpu * 3 / 2;
            //if (!(new[] { 2, 4, 8, 64, 256 }).Contains(cpu2)) BenchOne(times * cpu2, cpu2, rand);

            // 最大
            if (cpu < 64) BenchOne(times * cpu, 64, rand);
            if (cpu * 8 >= 256) BenchOne(times * cpu, cpu * 8, rand);
        }

        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        public virtual void BenchOne(Int64 times, Int32 threads, Boolean rand)
        {
            if (threads <= 0) threads = Environment.ProcessorCount;
            if (times <= 0) times = threads * 1_000;

            XTrace.WriteLine("");
            XTrace.WriteLine($"测试 {times:n0} 项，{threads,3:n0} 线程");

            var key = "Bench_";
            Set(key, Rand.NextBytes(32));
            var v = Get<Byte[]>(key);
            Remove(key);

            // 赋值测试
            BenchSet(key, times, threads, rand);

            // 读取测试
            BenchGet(key, times, threads, rand);

            // 删除测试
            BenchRemove(key, times, threads, rand);
        }

        /// <summary>读取测试</summary>
        /// <param name="key">键</param>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        protected virtual void BenchGet(String key, Int64 times, Int32 threads, Boolean rand)
        {
            var v = Get<Byte[]>(key);

            var sw = Stopwatch.StartNew();
            if (rand)
            {
                Parallel.For(0, threads, k =>
                {
                    for (var i = k; i < times; i += threads)
                    {
                        var val = Get<Byte[]>(key + i);
                    }
                });
            }
            else
            {
                Parallel.For(0, threads, k =>
                {
                    var mykey = key + k;
                    var count = times / threads;
                    for (var i = 0; i < count; i++)
                    {
                        var val = Get<Byte[]>(mykey);
                    }
                });
            }
            sw.Stop();

            var speed = times * 1000 / sw.ElapsedMilliseconds;
            XTrace.WriteLine($"读取 {times:n0} 项，{threads,3:n0} 线程，耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,9:n0} ops");
        }

        /// <summary>赋值测试</summary>
        /// <param name="key">键</param>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        protected virtual void BenchSet(String key, Int64 times, Int32 threads, Boolean rand)
        {
            //Set(key, Rand.NextBytes(32));

            var sw = Stopwatch.StartNew();
            if (rand)
            {
                Parallel.For(0, threads, k =>
                {
                    var val = Rand.NextBytes(32);
                    for (var i = k; i < times; i += threads)
                    {
                        Set(key + i, val);
                    }
                });
            }
            else
            {
                Parallel.For(0, threads, k =>
                {
                    var mykey = key + k;
                    var val = Rand.NextBytes(32);
                    var count = times / threads;
                    for (var i = 0; i < count; i++)
                    {
                        Set(mykey, val);
                    }
                });
            }
            sw.Stop();

            var speed = times * 1000 / sw.ElapsedMilliseconds;
            XTrace.WriteLine($"赋值 {times:n0} 项，{threads,3:n0} 线程，耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,9:n0} ops");
        }

        /// <summary>删除测试</summary>
        /// <param name="key">键</param>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        protected virtual void BenchRemove(String key, Int64 times, Int32 threads, Boolean rand)
        {
            Remove(key);

            var sw = Stopwatch.StartNew();
            if (rand)
            {
                Parallel.For(0, threads, k =>
                {
                    for (var i = k; i < times; i += threads)
                    {
                        Remove(key + i);
                    }
                });
            }
            else
            {
                Parallel.For(0, threads, k =>
                {
                    var mykey = key + k;
                    var count = times / threads;
                    for (var i = 0; i < count; i++)
                    {
                        Remove(mykey);
                    }
                });
            }
            sw.Stop();

            var speed = times * 1000 / sw.ElapsedMilliseconds;
            XTrace.WriteLine($"删除 {times:n0} 项，{threads,3:n0} 线程，耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,9:n0} ops");
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() { return Name; }
        #endregion
    }
}