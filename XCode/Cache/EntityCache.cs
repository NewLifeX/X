using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;
using XCode.DataAccessLayer;
using NewLife.Threading;

namespace XCode.Cache
{
    /// <summary>实体缓存</summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class EntityCache<TEntity> : CacheBase<TEntity>, IEntityCache where TEntity : Entity<TEntity>, new()
    {
        #region 基本
        private EntityList<TEntity> _Entities;
        /// <summary>实体集合。无数据返回空集合而不是null</summary>
        public EntityList<TEntity> Entities
        {
            get
            {
                XCache.CheckShowStatics(ref NextShow, ref Total, ShowStatics);

                // 两种情况更新缓存：1，缓存过期；2，不允许空但是集合又是空
                Boolean isnull = !AllowNull && _Entities == null;
                if (isnull || DateTime.Now > ExpireTime)
                {
                    lock (this)
                    {
                        isnull = !AllowNull && _Entities == null;
                        if (isnull || DateTime.Now > ExpireTime)
                        {
                            // 异步更新时，如果为空，表明首次，同步获取数据
                            if (Asynchronous && !isnull)
                            {
                                // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
                                // 设置时间放在获取缓存之前，让其它线程不要空等
                                ExpireTime = DateTime.Now.AddSeconds(Expriod);
                                Times++;

                                if (DAL.Debug)
                                {
                                    String reason = ExpireTime <= DateTime.MinValue ? "第一次" : (isnull ? "无缓存数据" : Expriod + "秒过期");
                                    DAL.WriteLog("异步更新实体缓存（第{2}次）：{0} 原因：{1}", typeof(TEntity).FullName, reason, Times);
                                }

                                ThreadPool.QueueUserWorkItem(FillWaper, isnull);
                            }
                            else
                            {
                                Times++;
                                if (DAL.Debug)
                                {
                                    String reason = ExpireTime <= DateTime.MinValue ? "第一次" : (isnull ? "无缓存数据" : Expriod + "秒过期");
                                    DAL.WriteLog("更新实体缓存（第{2}次）：{0} 原因：{1}", typeof(TEntity).FullName, reason, Times);
                                }

                                FillWaper(isnull);

                                // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
                                // 设置时间放在获取缓存之后，避免缓存尚未拿到，其它线程拿到空数据
                                ExpireTime = DateTime.Now.AddSeconds(Expriod);
                            }
                        }
                        else
                            Interlocked.Increment(ref Shoot2);
                    }
                }
                else
                    Interlocked.Increment(ref Shoot1);

                return _Entities ?? EntityList<TEntity>.Empty;
            }
        }

        private void FillWaper(Object state)
        {
            try
            {
                InvokeFill(delegate { _Entities = FillListMethod(); });

                // 清空
                if (_Entities != null && _Entities.Count < 1) _Entities = null;

                if (DAL.Debug) DAL.WriteLog("完成更新缓存（第{1}次）：{0}", typeof(TEntity).FullName, Times);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        private DateTime _ExpireTime;
        /// <summary>缓存过期时间</summary>
        public DateTime ExpireTime
        {
            get { return _ExpireTime; }
            set { _ExpireTime = value; }
        }

        /// <summary>缓存更新次数</summary>
        private Int64 Times;

        private Int32 _Expriod = 60;
        /// <summary>过期时间。单位是秒，默认60秒</summary>
        public Int32 Expriod
        {
            get { return _Expriod; }
            set { _Expriod = value; }
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

        private Boolean _Asynchronous;
        /// <summary>异步更新</summary>
        public Boolean Asynchronous
        {
            get { return _Asynchronous; }
            set { _Asynchronous = value; }
        }

        private Boolean _AllowNull;
        /// <summary>允许缓存空对象</summary>
        public Boolean AllowNull
        {
            get { return _AllowNull; }
            set { _AllowNull = value; }
        }

        /// <summary>清除缓存</summary>
        public void Clear()
        {
            if (_Entities != null && _Entities.Count > 0 && DAL.Debug) DAL.WriteLog("清空实体缓存：{0}", typeof(TEntity).FullName);

            ExpireTime = DateTime.Now;
            _Entities = null;
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

        /// <summary>
        /// 显示统计信息
        /// </summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("实体缓存<{0}>", typeof(TEntity).Name);
                sb.AppendFormat("总次数{0}", Total);
                if (Shoot1 > 0) sb.AppendFormat("，一级命中{0}（{1:P02}）", Shoot1, (Double)Shoot1 / Total);
                if (Shoot2 > 0) sb.AppendFormat("，二级命中{0}（{1:P02}）", Shoot2, (Double)Shoot2 / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region IEntityCache 成员
        EntityList<IEntity> IEntityCache.Entities
        {
            get
            {
                List<TEntity> old = Entities;
                if (old == null) return null;

                EntityList<IEntity> list = new EntityList<IEntity>();
                foreach (TEntity item in old)
                {
                    list.Add(item);
                }
                return list;
            }
        }

        /// <summary>
        /// 根据指定项查找
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public IEntity Find(string name, object value) { return Entities.Find(name, value); }

        /// <summary>
        /// 根据指定项查找
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public EntityList<IEntity> FindAll(string name, object value)
        {
            List<TEntity> old = Entities.FindAll(name, value);
            if (old == null) return null;

            EntityList<IEntity> list = new EntityList<IEntity>();
            foreach (TEntity item in old)
            {
                list.Add(item);
            }
            return list;
        }


        /// <summary>
        /// 检索与指定谓词定义的条件匹配的所有元素。
        /// </summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public EntityList<IEntity> FindAll(Predicate<IEntity> match)
        {
            List<TEntity> old = Entities.FindAll(e => match(e));
            if (old == null) return null;

            EntityList<IEntity> list = new EntityList<IEntity>();
            foreach (TEntity item in old)
            {
                list.Add(item);
            }
            return list;
        }
        #endregion
    }

    /// <summary>
    /// 填充数据的方法
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns></returns>
    public delegate EntityList<TEntity> FillListDelegate<TEntity>() where TEntity : Entity<TEntity>, new();
}