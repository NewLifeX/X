using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Threading;

namespace XCode.Cache
{
    /// <summary>实体缓存</summary>
    /// <remarks>
    /// 缓存更新逻辑：
    /// 1，初始化。首次访问阻塞等待，确保得到有效数据。
    /// 2，定时过期。过期后异步更新缓存返回旧数据，保障性能。但若过期两倍时间，则同步更新缓存阻塞等待返回新数据。
    /// 3，主动清除。外部主动清除缓存，强制清除后下次访问时同步更新缓存，非强制清除后下次访问时异步更新缓存。
    /// 4，添删改过期。添删改期间，仅修改缓存，不改变过期更新，避免事务中频繁更新缓存，提交回滚事务后强制清除缓存。
    /// </remarks>
    /// <typeparam name="TEntity">实体类型</typeparam>
    [DisplayName("实体缓存")]
    public class EntityCache<TEntity> : CacheBase<TEntity>, IEntityCache where TEntity : Entity<TEntity>, new()
    {
        #region 基础属性
        /// <summary>缓存过期时间</summary>
        DateTime ExpiredTime { get; set; } = DateTime.Now.AddHours(-1);

        private volatile Int32 _Times;
        /// <summary>缓存更新次数</summary>
        public Int32 Times => _Times;

        /// <summary>过期时间。单位是秒，默认10秒</summary>
        public Int32 Expire { get; set; }

        /// <summary>填充数据的方法</summary>
        public Func<IList<TEntity>> FillListMethod { get; set; } = Entity<TEntity>.FindAll;

        /// <summary>是否等待第一次查询。如果不等待，第一次返回空集合。默认true</summary>
        public Boolean WaitFirst { get; set; } = true;

        /// <summary>是否在使用缓存，在不触发缓存动作的情况下检查是否有使用缓存</summary>
        public Boolean Using { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化实体缓存</summary>
        public EntityCache()
        {
            var exp = Setting.Current.EntityCacheExpire;
            if (exp <= 0) exp = 10;

            Expire = exp;

            LogPrefix = $"EntityCache<{typeof(TEntity).Name}>";
        }
        #endregion

        #region 缓存核心
        private TEntity[] _Entities = new TEntity[0];
        /// <summary>实体集合。无数据返回空集合而不是null</summary>
        public IList<TEntity> Entities
        {
            get
            {
                CheckCache();

                return _Entities;
            }
        }

        void CheckCache()
        {
            // 更新统计信息
            CheckShowStatics(ref Total, ShowStatics);

            var sec = (TimerX.Now - ExpiredTime).TotalSeconds;
            if (sec < 0)
            {
                Interlocked.Increment(ref Success);
                return;
            }

            /*
             * 来到这里，都是缓存超时的线程
             * 1，第一次请求，还没有缓存数据，需要等待首次数据
             *      WaitFirst   加锁等待
             *      !WaitFirst  跳过等待
             * 2，缓存过期，开个异步更新，继续走
             */

            if (_Times == 0)
            {
                if (WaitFirst)
                {
                    lock (this)
                    {
                        if (_Times == 0) UpdateCache("第一次");
                    }
                }
                else
                {
                    if (Monitor.TryEnter(this, 5000))
                    {
                        try
                        {
                            if (_Times == 0) UpdateCache("第一次");
                        }
                        finally
                        {
                            Monitor.Exit(this);
                        }
                    }
                }
            }
            else
            {
                var s = ExpiredTime == DateTime.MinValue ? "已强制过期" : $"已过期{sec:n2}秒";
                var msg = $"有效期{Expire}秒，{s}";

                // 频繁更新下，采用异步更新缓存，以提升吞吐。非频繁访问时（2倍超时），同步更新
                if (sec < Expire)
                    UpdateCacheAsync(msg);
                else
                    UpdateCache(msg);
            }

            // 只要访问了实体缓存数据集合，就认为是使用了实体缓存，允许更新缓存数据期间向缓存集合添删数据
            Using = true;
        }

        /// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public TEntity Find(Predicate<TEntity> match)
        {
            var list = Entities;
            if (list is List<TEntity> list2) return list2.Find(match);

            return list.FirstOrDefault(e => match(e));
        }

        /// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public IList<TEntity> FindAll(Predicate<TEntity> match)
        {
            var list = Entities;
            if (list is List<TEntity> list2) return list2.FindAll(match);

            return list.Where(e => match(e)).ToList();
        }
        #endregion

        #region 缓存操作
        private volatile Int32 _updating;

        void UpdateCacheAsync(String reason)
        {
            // 控制只有一个线程能更新
            if (Interlocked.CompareExchange(ref _updating, 1, 0) != 0) return;

            WriteLog($"异步更新缓存，{reason}");
            ThreadPoolX.QueueUserWorkItem(UpdateCache, reason);
        }

        void UpdateCache(String reason)
        {
            Interlocked.Increment(ref _updating);

            var ts = _Times;

            // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
            // 设置时间放在获取缓存之前，让其它线程不要空等
            if (ts > 0) ExpiredTime = TimerX.Now.AddSeconds(Expire);

            WriteLog("更新（第{0}次） 原因：{1}", ts + 1, reason);

            try
            {
                _Entities = Invoke<Object, IList<TEntity>>(s => FillListMethod(), null).ToArray();
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"[{TableName}/{ConnName}]" + ex.GetTrue());
            }
            finally
            {
                _updating = 0;
            }

            ts = Interlocked.Increment(ref _Times);
            ExpiredTime = TimerX.Now.AddSeconds(Expire);
            WriteLog("完成[{0}]（第{1}次）", _Entities.Length, ts);
        }

        /// <summary>清除缓存</summary>
        /// <param name="reason">清除原因</param>
        /// <param name="force">强制清除，下次访问阻塞等待。默认false仅置为过期，下次访问异步更新</param>
        public void Clear(String reason, Boolean force = false)
        {
            if (!Using) return;

            //// 直接执行异步更新，明明白白，确保任何情况下数据最新，并且不影响其它任务的性能
            //UpdateCacheAsync(reason);

            // 强迫下一次访问阻塞等待刷新
            if (force)
            {
                ExpiredTime = DateTime.MinValue;
                WriteLog("强制清除缓存，下次访问时同步更新阻塞等待");
            }
            else
            {
                ExpiredTime = DateTime.Now;
                WriteLog("非强制清除缓存，下次访问时异步更新无需阻塞等待");
            }
        }

        private readonly IEntityFactory _factory = Entity<TEntity>.Meta.Factory;
        /// <summary>添加对象到缓存</summary>
        /// <param name="entity"></param>
        public void Add(TEntity entity)
        {
            if (!Using) return;

            var es = _Entities;
            lock (es)
            {
                var list = _Entities.ToList();
                list.Add(entity);
                _Entities = list.ToArray();
            }
        }

        /// <summary>从缓存中删除对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public TEntity Remove(TEntity entity)
        {
            if (!Using) return null;

            var es = _Entities;

            var e = es.Find(x => x == entity);
            if (e == null)
            {
                var fi = _factory.Unique;
                if (fi != null)
                {
                    var v = entity[fi.Name];
                    e = es.Find(x => Equals(x[fi.Name], v));
                }
            }
            if (e == null) return null;

            // 更新实体缓存时，不做拷贝，避免产生脏数据，如果恰巧又使用单对象缓存，那会导致自动保存
            lock (es)
            {
                var list = _Entities.ToList();
                list.Remove(e);
                _Entities = list.ToArray();
            }

            return e;
        }

        internal TEntity Update(TEntity entity)
        {
            if (!Using) return null;

            var es = _Entities;

            // 如果对象本身就在缓存里面，啥也不用做
            var e = es.FirstOrDefault(x => x == entity);
            if (e != null) return e;

            var idx = -1;
            var fi = _factory.Unique;
            if (fi != null)
            {
                var v = entity[fi.Name];
                idx = Array.FindIndex(es, x => Equals(x[fi.Name], v));
            }

            // 更新实体缓存时，不做拷贝，避免产生脏数据，如果恰巧又使用单对象缓存，那会导致自动保存
            if (idx >= 0)
                es[idx] = entity;
            else
            {
                lock (es)
                {
                    var list = _Entities.ToList();
                    list.Add(entity);
                    _Entities = list.ToArray();
                }
            }

            return e;
        }
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public Int32 Total;

        /// <summary>命中</summary>
        public Int32 Success;

        /// <summary>显示统计信息</summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = Pool.StringBuilder.Get();
                var type = GetType();
                var name = $"{type.GetDisplayName() ?? type.Name}<{typeof(TEntity).Name}>({_Entities.Length:n0})";
                sb.AppendFormat("{0,-24}", name);
                sb.AppendFormat("总次数{0,11:n0}", Total);
                if (Success > 0) sb.AppendFormat("，命中{0,11:n0}（{1,6:P02}）", Success, (Double)Success / Total);
                sb.AppendFormat("\t[{0}]", typeof(TEntity).FullName);

                XTrace.WriteLine(sb.Put(true));
            }
        }
        #endregion

        #region IEntityCache 成员
        IList<IEntity> IEntityCache.Entities => new List<IEntity>(Entities);
        #endregion

        #region 辅助
        internal EntityCache<TEntity> CopySettingFrom(EntityCache<TEntity> ec)
        {
            Expire = ec.Expire;
            FillListMethod = ec.FillListMethod;

            return this;
        }

        /// <summary>输出名称</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var type = GetType();
            return $"{type.GetDisplayName() ?? type.Name}<{typeof(TEntity).Name}>";
        }
        #endregion
    }
}