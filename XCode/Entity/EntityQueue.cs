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

        /// <summary>用于保存数据的实体会话</summary>
        public IEntitySession Session { get; set; }

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
            if (_Timer == null) _Timer = new TimerX(Work, null, Period, Period, true);

            lock (this)
            {
                Entities.Add(entity);
            }

            return true;
        }

        private void Work(Object state)
        {
            if (Entities.Count == 0) return;

            IEntity[] es = null;
            lock (this)
            {
                es = Entities.ToArray();
                Entities.Clear();
            }
            if (es.Length == 0) return;

            var ss = Session;

            //var cfg = Setting.Current;
            if (XTrace.Debug) XTrace.WriteLine("实体队列[{0}@{1}]准备持久化{2}个对象", ss.TableName, ss.ConnName, es.Length);

            var rs = new List<Int32>();
            var sw = new Stopwatch();
            sw.Start();

            // 开启事务保存
            ss.BeginTrans();
            try
            {
                foreach (var item in es)
                {
                    rs.Add(item.Save());
                }

                ss.Commit();

                sw.Stop();
            }
            catch
            {
                ss.Rollback();
                throw;
            }

            if (XTrace.Debug) XTrace.WriteLine("实体队列[{0}@{1}]持久化{2}个对象共耗时 {3}", ss.TableName, ss.ConnName, es.Length, sw.Elapsed);

            // 根据繁忙程度动态调节
            // 大于1000个对象时，说明需要加快持久化间隔，缩小周期
            // 小于1000个对象时，说明持久化太快了，加大周期
            var p = Period;
            if (es.Length > 1000)
                p = p * 1000 / es.Length;
            else
                p = p * 1000 / es.Length;

            // 最小间隔1000毫秒
            if (p < 1000) p = 1000;

            if (p != Period)
            {
                Period = p;
                _Timer.Period = p;
            }

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