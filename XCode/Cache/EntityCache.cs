using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>实体缓存</summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class EntityCache<TEntity> : CacheBase<TEntity>, IEntityCache where TEntity : Entity<TEntity>, new()
    {
        #region 基本
        private DateTime _ExpiredTime;
        /// <summary>缓存过期时间</summary>
        public DateTime ExpiredTime { get { return _ExpiredTime; } set { _ExpiredTime = value; } }

        /// <summary>缓存更新次数</summary>
        private Int64 Times;

        private Int32 _Expriod = 60;
        /// <summary>过期时间。单位是秒，默认60秒</summary>
        public Int32 Expriod { get { return _Expriod; } set { _Expriod = value; } }

        private Boolean _Asynchronous;
        /// <summary>异步更新</summary>
        public Boolean Asynchronous { get { return _Asynchronous; } set { _Asynchronous = value; } }

        private Boolean _AllowNull = true;
        /// <summary>允许缓存空对象</summary>
        public Boolean AllowNull { get { return _AllowNull; } set { _AllowNull = value; } }

        private Boolean _Using;
        /// <summary>是否在使用缓存</summary>
        internal Boolean Using { get { return _Using; } private set { _Using = value; } }
        #endregion

        #region 缓存核心
        private EntityList<TEntity> _Entities;
        /// <summary>实体集合。无数据返回空集合而不是null</summary>
        public EntityList<TEntity> Entities
        {
            get
            {
                XCache.CheckShowStatics(ref NextShow, ref Total, ShowStatics);

                // 两种情况更新缓存：1，缓存过期；2，不允许空但是集合又是空
                Boolean isnull = !AllowNull && _Entities == null;
                if (isnull || DateTime.Now >= ExpiredTime)
                {
                    lock (this)
                    {
                        isnull = !AllowNull && _Entities == null;
                        if (isnull || DateTime.Now >= ExpiredTime)
                            UpdateCache(isnull);
                        else
                            Interlocked.Increment(ref Shoot2);
                    }
                }
                else
                    Interlocked.Increment(ref Shoot1);

                Using = true;

                return _Entities ?? new EntityList<TEntity>();
            }
        }

        private FillListDelegate<TEntity> _FillListMethod;
        /// <summary>填充数据的方法</summary>
        public FillListDelegate<TEntity> FillListMethod
        {
            get
            {
                if (_FillListMethod == null) _FillListMethod = Entity<TEntity>.FindAll;
                return _FillListMethod;
            }
            set { _FillListMethod = value; }
        }
        #endregion

        #region 缓存操作
        void UpdateCache(Boolean isnull)
        {
            // 异步更新时，如果为空，表明首次，同步获取数据
            if (Asynchronous && !isnull)
            {
                // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
                // 设置时间放在获取缓存之前，让其它线程不要空等
                ExpiredTime = DateTime.Now.AddSeconds(Expriod);
                Times++;

                if (Debug)
                {
                    var reason = ExpiredTime <= DateTime.MinValue ? "第一次" : (isnull ? "无缓存数据" : Expriod + "秒过期");
                    DAL.WriteLog("异步更新实体缓存（第{2}次）：{0} 原因：{1}", typeof(TEntity).FullName, reason, Times);
                }

                ThreadPool.QueueUserWorkItem(FillWaper, isnull);
            }
            else
            {
                Times++;
                if (Debug)
                {
                    var reason = ExpiredTime <= DateTime.MinValue ? "第一次" : (isnull ? "无缓存数据" : Expriod + "秒过期");
                    DAL.WriteLog("更新实体缓存（第{2}次）：{0} 原因：{1} {3}", typeof(TEntity).FullName, reason, Times, XTrace.GetCaller(2, 2));
                }

                FillWaper(isnull);

                // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
                // 设置时间放在获取缓存之后，避免缓存尚未拿到，其它线程拿到空数据
                ExpiredTime = DateTime.Now.AddSeconds(Expriod);
            }
        }

        private void FillWaper(Object state)
        {
            try
            {
                InvokeFill(delegate { _Entities = FillListMethod(); });

                // HUIYUE 2012.12.08
                // 注释掉这句，这句会导致在 _Entities.Count = 0 的情况下多次调用EntityList<TEntity>.Empty
                // 使得 EntityList<TEntity>.Empty 被赋值，然后就杯具了
                // 清空
                //if (_Entities != null && _Entities.Count < 1) _Entities = null;

                if (Debug) DAL.WriteLog("完成更新缓存（第{1}次）：{0}", typeof(TEntity).FullName, Times);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        /// <summary>清除缓存</summary>
        public void Clear(String reason = null)
        {
            lock (this)
            {
                if (_Entities != null && _Entities.Count > 0 && Debug) DAL.WriteLog("清空实体缓存：{0} 原因：{1}", typeof(TEntity).FullName, reason);

                // 修改为最小，确保过期
                ExpiredTime = DateTime.MinValue;
                _Entities = null;
            }
        }
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public Int32 Total;

        /// <summary>第一次命中</summary>
        public Int32 Shoot1;

        /// <summary>第二次命中</summary>
        public Int32 Shoot2;

        /// <summary>下一次显示时间</summary>
        public DateTime NextShow;

        /// <summary>显示统计信息</summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("实体缓存<{0}>", typeof(TEntity).Name);
                sb.AppendFormat("总次数{0}", Total);
                if (Shoot1 > 0) sb.AppendFormat("，命中{0}（{1:P02}）", Shoot1, (Double)Shoot1 / Total);
                if (Shoot2 > 0) sb.AppendFormat("，二级命中{0}（{1:P02}）", Shoot2, (Double)Shoot2 / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region IEntityCache 成员
        EntityList<IEntity> IEntityCache.Entities { get { return new EntityList<IEntity>(Entities); } }

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public IEntity Find(string name, object value) { return Entities.Find(name, value); }

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public EntityList<IEntity> FindAll(string name, object value) { return new EntityList<IEntity>(Entities.FindAll(name, value)); }

        /// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public EntityList<IEntity> FindAll(Predicate<IEntity> match) { return new EntityList<IEntity>(Entities.FindAll(e => match(e))); }
        #endregion
    }

    /// <summary>填充数据的方法</summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns></returns>
    public delegate EntityList<TEntity> FillListDelegate<TEntity>() where TEntity : Entity<TEntity>, new();
}