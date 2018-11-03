using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Threading;

namespace XCode.Cache
{
    /// <summary>实体缓存</summary>
    /// <remarks>
    /// 第一次读取缓存的时候，同步从数据库读取，这样子手上有一份数据。
    /// 以后更新，都开异步线程去读取，而当前马上返回，让大家继续用着旧数据，这么做性能非常好。
    /// </remarks>
    /// <typeparam name="TEntity">实体类型</typeparam>
    [DisplayName("实体缓存")]
    public class EntityCache<TEntity> : CacheBase<TEntity>, IEntityCache where TEntity : Entity<TEntity>, new()
    {
        #region 基础属性
        /// <summary>缓存过期时间</summary>
        DateTime ExpiredTime { get; set; } = DateTime.Now.AddHours(-1);

        /// <summary>缓存更新次数</summary>
        private Int64 Times { get; set; }

        /// <summary>过期时间。单位是秒，默认60秒</summary>
        public Int32 Expire { get; set; }

        /// <summary>填充数据的方法</summary>
        public Func<IList<TEntity>> FillListMethod { get; set; } = Entity<TEntity>.FindAll;

        /// <summary>是否等待第一次查询。如果不等待，第一次返回空集合。默认true</summary>
        public Boolean WaitFirst { get; set; } = true;

        /// <summary>是否在使用缓存，在不触发缓存动作的情况下检查是否有使用缓存</summary>
        internal Boolean Using { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化实体缓存</summary>
        public EntityCache()
        {
            var exp = Setting.Current.EntityCacheExpire;
            if (exp <= 0) exp = 60;
            Expire = exp;
        }
        #endregion

        #region 缓存核心
        private IList<TEntity> _Entities = new List<TEntity>();
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

            if (Times == 0)
            {
                if (WaitFirst)
                {
                    lock (this)
                    {
                        if (Times == 0) UpdateCache("第一次");
                    }
                }
                else
                {
                    if (Monitor.TryEnter(this, 5000))
                    {
                        try
                        {
                            if (Times == 0) UpdateCache("第一次");
                        }
                        finally
                        {
                            Monitor.Exit(this);
                        }
                    }
                }
            }
            else
                UpdateCacheAsync("过期{0:n0}秒".F(sec));

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

            ThreadPoolX.QueueUserWorkItem(UpdateCache, reason);
        }

        void UpdateCache(String reason)
        {
            Interlocked.Increment(ref _updating);

            // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
            // 设置时间放在获取缓存之前，让其它线程不要空等
            if (Times > 0) ExpiredTime = TimerX.Now.AddSeconds(Expire);

            WriteLog("更新{0}（第{2}次） 原因：{1}", ToString(), reason, Times + 1);

            try
            {
                _Entities = Invoke<Object, IList<TEntity>>(s => FillListMethod(), null);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"[{TableName}/{ConnName}]" + ex.GetTrue());
            }
            finally
            {
                _updating = 0;
            }

            Times++;
            ExpiredTime = TimerX.Now.AddSeconds(Expire);
            WriteLog("完成{0}[{1}]（第{2}次）", ToString(), _Entities.Count, Times);
        }

        /// <summary>清除缓存</summary>
        public void Clear(String reason)
        {
            if (!Using) return;

            // 直接执行异步更新，明明白白，确保任何情况下数据最新，并且不影响其它任务的性能
            UpdateCacheAsync(reason);
        }

        private IEntityOperate Operate = Entity<TEntity>.Meta.Factory;
        internal void Add(TEntity entity)
        {
            if (!Using) return;

            var es = _Entities;
            lock (es)
            {
                es.Add(entity);
            }
        }

        internal TEntity Remove(TEntity entity)
        {
            if (!Using) return null;

            var es = _Entities;
            var fi = Operate.Unique;
            if (fi == null) return null;

            var e = es.Find(x => x == entity);
            if (e == null)
            {
                var v = entity[fi.Name];
                e = es.Find(x => Equals(x[fi.Name], v));
            }
            if (e == null) return null;

            //if (e != entity) e.CopyFrom(entity);
            // 更新实体缓存时，不做拷贝，避免产生脏数据，如果恰巧又使用单对象缓存，那会导致自动保存
            lock (es)
            {
                es.Remove(e);
            }

            return e;
        }

        internal TEntity Update(TEntity entity)
        {
            if (!Using) return null;

            var rs = Remove(entity);
            Add(entity);

            return rs;
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
                var name = "{2}<{0}>({1:n0})".F(typeof(TEntity).Name, _Entities.Count, type.GetDisplayName() ?? type.Name);
                sb.AppendFormat("{0,-24}", name);
                sb.AppendFormat("总次数{0,11:n0}", Total);
                if (Success > 0) sb.AppendFormat("，命中{0,11:n0}（{1,6:P02}）", Success, (Double)Success / Total);
                sb.AppendFormat("\t[{0}]", typeof(TEntity).FullName);

                XTrace.WriteLine(sb.Put(true));
            }
        }
        #endregion

        #region IEntityCache 成员
        IList<IEntity> IEntityCache.Entities { get { return new List<IEntity>(Entities); } }
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
            return "{0}<{1}>".F(type.GetDisplayName() ?? type.Name, typeof(TEntity).Name);
        }
        #endregion
    }
}