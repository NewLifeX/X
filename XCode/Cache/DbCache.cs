using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Collections;
using NewLife.Serialization;
using NewLife.Threading;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Extension;

namespace NewLife.Caching
{
    /// <summary>数据库缓存。利用数据表来缓存信息</summary>
    /// <remarks>
    /// 构建一个操作队列，新增、更新、删除等操作全部排队单线程执行，以改进性能
    /// </remarks>
    public class DbCache : NewLife.Caching.Cache
    {
        #region 属性
        /// <summary>实体工厂</summary>
        protected IEntityOperate Factory { get; }

        /// <summary>主键字段</summary>
        protected Field KeyField { get; }

        /// <summary>时间字段</summary>
        protected Field TimeField { get; }
        #endregion

        #region 构造
        /// <summary>实例化一个数据库缓存</summary>
        /// <param name="factory"></param>
        /// <param name="keyName"></param>
        /// <param name="timeName"></param>
        public DbCache(IEntityOperate factory = null, String keyName = null, String timeName = null)
        {
            if (factory == null) factory = MyDbCache.Meta.Factory;
            if (!(factory.Default is IDbCache)) throw new XCodeException("实体类[{0}]需要实现[{1}]接口", factory.EntityType.FullName, typeof(IDbCache).FullName);

            var name = factory.EntityType.Name;

            var key = !keyName.IsNullOrEmpty() ? factory.Table.FindByName(keyName) : factory.Unique;
            if (key == null || key.Type != typeof(String)) throw new XCodeException("[{0}]没有字符串类型的主键".F(name));

            TimeField = (!timeName.IsNullOrEmpty() ? factory.Table.FindByName(timeName) : factory.MasterTime) as Field;

            Factory = factory;
            KeyField = key as Field;
            Name = name;

            // 关闭日志
            var db = factory.Session.Dal.Db;
            db.ShowSQL = false;
            (db as DbBase).TraceSQLTime *= 10;

            Init(null);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            clearTimer.TryDispose();
            clearTimer = null;
        }
        #endregion

        #region 属性
        /// <summary>缓存个数。高频使用时注意性能</summary>
        public override Int32 Count => Factory.Count;

        /// <summary>所有键。实际返回只读列表新实例，数据量较大时注意性能</summary>
        public override ICollection<String> Keys => Factory.FindAll().Select(e => e[Factory.Unique] as String).ToList();
        #endregion

        #region 方法
        /// <summary>初始化配置</summary>
        /// <param name="config"></param>
        public override void Init(String config)
        {
            if (clearTimer == null)
            {
                var period = 60;
                clearTimer = new TimerX(RemoveNotAlive, null, period * 1000, period * 1000) { Async = true };
            }
        }

        private DictionaryCache<String, IDbCache> _cache = new DictionaryCache<String, IDbCache>()
        {
            Expire = 60,
            AllowNull = false,
        };
        private IDbCache Find(String key)
        {
            if (key.IsNullOrEmpty()) return null;

            if (_cache.FindMethod == null) _cache.FindMethod = k => Factory.Find(KeyField == key) as IDbCache;

            return _cache[key];
        }
        #endregion

        #region 基本操作
        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Boolean ContainsKey(String key) => Find(key) != null;

        /// <summary>添加缓存项，已存在时更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间</param>
        /// <returns></returns>
        public override Boolean Set<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            var e = Find(key);
            if (e == null)
            {
                //e = Factory.GetOrAdd(key) as IDbCache;
                e = Factory.Create() as IDbCache;
                e.Name = key;
                if (e != null) _cache[key] = e;
            }
            e.Value = value.ToJson();
            e.ExpiredTime = TimerX.Now.AddSeconds(expire);

            if (e.CreateTime.Year < 2000) e.CreateTime = TimerX.Now;
            e.SaveAsync();

            return true;
        }

        /// <summary>获取缓存项，不存在时返回默认值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override T Get<T>(String key)
        {
            var e = Find(key);
            if (e == null) return default(T);

            var value = e.Value;
            //return JsonHelper.Convert<T>(value);
            //if (typeof(T) == typeof(Byte[])) return (T)(Object)(value + "").ToBase64();
            if (typeof(T) == typeof(String)) return (T)(Object)value;

            //return value.ChangeType<T>();
            return value.ToJsonEntity<T>();
        }

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns>实际移除个数</returns>
        public override Int32 Remove(params String[] keys)
        {
            if (Count == 0) return 0;

            var count = 0;
            foreach (var item in keys)
            {
                var e = _cache.Get(item);
                if (e != null)
                {
                    _cache.Remove(item);

                    (e as IEntity).Delete();

                    count++;
                }
            }
            return count;

            //var list = Factory.FindAll(KeyField.In(keys), null, null, 0, 0);
            //foreach (IDbCache item in list)
            //{
            //    _cache.Remove(item.Name);
            //}
            //return list.Delete();
        }

        /// <summary>删除所有配置项</summary>
        public override void Clear() => Factory.Session.Truncate();

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        /// <returns>设置是否成功</returns>
        public override Boolean SetExpire(String key, TimeSpan expire)
        {
            var e = Find(key);
            if (e == null) return false;

            e.ExpiredTime = TimerX.Now.Add(expire);
            e.SaveAsync();

            return true;
        }

        /// <summary>获取缓存项有效期，不存在时返回Zero</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override TimeSpan GetExpire(String key)
        {
            var e = Find(key);
            if (e == null) return TimeSpan.Zero;

            return e.ExpiredTime - TimerX.Now;
        }
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新，常用于锁争夺</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间</param>
        /// <returns></returns>
        public override Boolean Add<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            var e = Find(key);
            if (e != null) return false;

            e = Factory.Create() as IDbCache;
            e.Name = key;
            e.Value = value.ToJson();
            e.ExpiredTime = TimerX.Now.AddSeconds(expire);
            (e as IEntity).Insert();

            _cache[key] = e;

            return true;
        }
        #endregion

        #region 清理过期缓存
        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        /// <summary>移除过期的缓存项</summary>
        void RemoveNotAlive(Object state)
        {
            // 这里先计算，性能很重要
            var now = TimerX.Now;
            var list = Factory.FindAll(TimeField < now, null, null, 0, 0);
            foreach (IDbCache item in list)
            {
                _cache.Remove(item.Name);
            }
            list.Delete();
        }
        #endregion

        #region 性能测试
        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        public override void BenchOne(Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            if (rand)
                times *= 1;
            else
                times *= 1000;

            base.BenchOne(times, threads, rand, batch);
        }
        #endregion
    }

    /// <summary>数据缓存接口</summary>
    public interface IDbCache
    {
        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>键值</summary>
        String Value { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>过期时间</summary>
        DateTime ExpiredTime { get; set; }

        /// <summary>异步保存</summary>
        /// <param name="msDelay"></param>
        /// <returns></returns>
        Boolean SaveAsync(Int32 msDelay = 0);
    }
}