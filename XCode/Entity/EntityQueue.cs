using System;
using System.Collections.Generic;
using System.Diagnostics;
using NewLife;
using NewLife.Log;
using NewLife.Threading;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体队列</summary>
    public class EntityQueue
    {
        #region 属性
        private IList<IEntity> Entities { get; set; }

        ///// <summary>用于保存数据的实体会话</summary>
        //public IEntitySession Session { get; set; }

        /// <summary>数据访问</summary>
        public DAL Dal { get; set; }

        /// <summary>周期。默认1000毫秒，根据繁忙程度动态调节，尽量靠近每次持久化1000个对象</summary>
        public Int32 Period { get; set; }

        /// <summary>完成事件。</summary>
        public event EventHandler<EventArgs<IEntity, Int32>> Completed;

        private TimerX _Timer;
        #endregion

        #region 构造
        /// <summary>实例化实体队列</summary>
        public EntityQueue()
        {
            Entities = new List<IEntity>();

            Period = 10000;
        }
        #endregion

        #region 方法
        /// <summary>添加实体对象进入队列</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Boolean Add(IEntity entity)
        {
            if (_Timer == null) _Timer = new TimerX(Work, null, Period, Period);

            lock (this)
            {
                Entities.Add(entity);
            }

            return true;
        }

        private void Work(Object state)
        {
            if (_Running) return;
            if (Entities.Count == 0) return;

            IEntity[] es = null;
            lock (this)
            {
                es = Entities.ToArray();
                Entities.Clear();
            }
            if (es.Length == 0) return;

            _Running = true;
            ThreadPoolX.QueueUserWorkItem(Process, es);
        }

        private Boolean _Running;
        private void Process(Object state)
        {
            var es = state as IEntity[];
            var dal = Dal;

            //var cfg = Setting.Current;
            if (XTrace.Debug) XTrace.WriteLine("实体队列[{0}]\t准备持久化{1}个对象", dal.ConnName, es.Length);

            var rs = new List<Int32>();
            var sw = new Stopwatch();
            sw.Start();

            // 开启事务保存
            dal.BeginTransaction();
            try
            {
                foreach (var item in es)
                {
                    rs.Add(item.Save());
                }

                dal.Commit();

                sw.Stop();
            }
            catch
            {
                dal.Rollback();
                throw;
            }
            finally
            {
                _Running = false;
            }

            // 根据繁忙程度动态调节
            // 大于1000个对象时，说明需要加快持久化间隔，缩小周期
            // 小于1000个对象时，说明持久化太快了，加大周期
            var p = Period;
            if (es.Length > 1000)
                p = p * 1000 / es.Length;
            else
                p = p * 1000 / es.Length;

            // 最小间隔100毫秒
            if (p < 100) p = 100;
            // 最大间隔3秒
            if (p > 3000) p = 3000;

            if (p != Period)
            {
                Period = p;
                _Timer.Period = p;
            }

            if (XTrace.Debug) XTrace.WriteLine("实体队列[{0}]\t共耗时 {1:n0}毫秒\t周期 {2:n0}毫秒", dal.ConnName, sw.ElapsedMilliseconds, p);

            if (Completed != null)
            {
                for (int i = 0; i < es.Length; i++)
                {
                    Completed(this, new EventArgs<IEntity, int>(es[i], rs[i]));
                }
            }
        }
        #endregion
    }
}