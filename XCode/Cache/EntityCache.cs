using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;

namespace XCode.Cache
{
    /// <summary>
    /// 实体缓存
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class EntityCache<TEntity> : CacheBase<TEntity> where TEntity : Entity<TEntity>, new()
    {
        #region 基本
        private EntityList<TEntity> _Entities;
        /// <summary>实体集合</summary>
        public EntityList<TEntity> Entities
        {
            get
            {
                #region 统计
                if (LastShow == DateTime.MinValue) LastShow = DateTime.Now;
                if (LastShow.AddHours(10) < DateTime.Now)
                {
                    LastShow = DateTime.Now;

                    ShowStatics();
                }

                Interlocked.Increment(ref Total);
                #endregion

                if (DateTime.Now > CacheTime.AddSeconds(Expriod))
                {
                    lock (this)
                    {
                        if (DateTime.Now > CacheTime.AddSeconds(Expriod))
                        {
                            CacheTime = DateTime.Now;
                            if (Asynchronous)
                                ThreadPool.QueueUserWorkItem(new WaitCallback(FillWaper));
                            else
                                InvokeFill(delegate { _Entities = FillListMethod(); });
                        }
                        else
                            Interlocked.Increment(ref Shoot2);
                    }
                }
                else
                    Interlocked.Increment(ref Shoot1);

                if (_Entities == null || _Entities.Count < 1) return new EntityList<TEntity>();
                return _Entities;
            }
            //set { _Entities = value; }
        }

        private void FillWaper(Object state)
        {
            try
            {
                InvokeFill(delegate { _Entities = FillListMethod(); });
                CacheTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
        }

        private DateTime _CacheTime = DateTime.Now.AddDays(-100);
        /// <summary>缓存时间</summary>
        public DateTime CacheTime
        {
            get { return _CacheTime; }
            set { _CacheTime = value; }
        }

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

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            CacheTime = DateTime.Now.AddDays(-100);
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

        /// <summary>最后显示时间</summary>
        public DateTime LastShow;

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
    }

    /// <summary>
    /// 填充数据的方法
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns></returns>
    public delegate EntityList<TEntity> FillListDelegate<TEntity>() where TEntity : Entity<TEntity>, new();
}