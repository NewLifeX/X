using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;

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

                    if (XTrace.Debug) XTrace.WriteLine("发现缓存实现 [{0}] = {1}", id, item.FullName);

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
                if (type == null) throw new ArgumentNullException(nameof(type), "找不到名为[{0}]的缓存实现".F(id));

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

            var item = CacheConfig.Current.GetOrAdd(name);
            return Create(item);
        }
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; protected set; }

        /// <summary>默认缓存时间。默认365*24*3600秒</summary>
        public Int32 Expire { get; set; } = 365 * 24 * 3600;

        /// <summary>获取和设置缓存，永不过期</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Object this[String key] { get { return Get<Object>(key); } set { Set(key, value); } }

        /// <summary>缓存个数</summary>
        public abstract Int32 Count { get; }

        /// <summary>所有键</summary>
        public abstract ICollection<String> Keys { get; }
        #endregion

        #region 方法
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
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public abstract Boolean Set<T>(String key, T value, Int32 expire = 0);

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

        #region 高级操作
        /// <summary>批量获取缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual IDictionary<String, T> GetAll<T>(params String[] keys)
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
        public virtual void SetAll<T>(IDictionary<String, T> values)
        {
            foreach (var item in values)
            {
                Set(item.Key, item.Value);
            }
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public virtual Int32 Increment(String key, Int32 amount)
        {
            lock (this)
            {
                var v = Get<Int32>(key);
                v += amount;
                Set(key, v);

                return v;
            }
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public virtual Int32 Decrement(String key, Int32 amount)
        {
            lock (this)
            {
                var v = Get<Int32>(key);
                v -= amount;
                Set(key, v);

                return v;
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

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() { return Name; }
        #endregion
    }
}