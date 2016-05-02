using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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
        private ICollection<IEntity> Entities { get; set; }

        /// <summary>调试开关，默认false</summary>
        public Boolean Debug { get; set; }

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
            Entities = new HashSet<IEntity>();

            Period = 1000;
        }
        #endregion

        #region 方法
        /// <summary>添加实体对象进入队列</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Boolean Add(IEntity entity)
        {
            if (_Timer == null) _Timer = new TimerX(Work, null, Period, Period);

            // 避免重复加入队列
            var list = Entities;
            if (list.Contains(entity)) return false;

            lock (this)
            {
                list = Entities;
                // 避免重复加入队列
                if (list.Contains(entity)) return false;

                list.Add(entity);
            }

            return true;
        }

        private void Work(Object state)
        {
            if (_Running) return;

            var list = Entities;
            if (list.Count == 0) return;

            //var es = list;
            lock (this)
            {
                //es = list.ToArray();
                //list.Clear();

                // 为了速度，不拷贝，直接创建一个新的集合
                list = Entities;
                if (list.Count == 0) return;

                Entities = new HashSet<IEntity>();
            }
            if (list.Count == 0) return;

            _Running = true;
            Task.Factory.StartNew(Process, list);
        }

        private Boolean _Running;
        private void Process(Object state)
        {
            var list = state as ICollection<IEntity>;
            var dal = Dal;

            if (Debug) XTrace.WriteLine("实体队列[{0}]\t准备持久化{1}个对象", dal.ConnName, list.Count);

            var rs = new List<Int32>();
            var sw = new Stopwatch();
            if (Debug) sw.Start();

            // 开启事务保存
            dal.BeginTransaction();
            try
            {
                foreach (var item in list)
                {
                    rs.Add(item.Save());
                }

                dal.Commit();

                if (Debug) sw.Stop();
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
            if (list.Count > 1000)
                p = p * 1000 / list.Count;
            else
                p = p * 1000 / list.Count;

            // 最小间隔100毫秒
            if (p < 100) p = 100;
            // 最大间隔3秒
            if (p > 3000) p = 3000;

            if (p != Period)
            {
                Period = p;
                _Timer.Period = p;
            }

            if (Debug) XTrace.WriteLine("实体队列[{0}]\t共耗时 {1:n0}毫秒\t周期 {2:n0}毫秒", dal.ConnName, sw.ElapsedMilliseconds, p);

            if (Completed != null)
            {
                //for (int i = 0; i < list.Count; i++)
                //{
                //    Completed(this, new EventArgs<IEntity, int>(list[i], rs[i]));
                //}
                var k = 0;
                foreach (var item in list)
                {
                    Completed(this, new EventArgs<IEntity, int>(item, rs[k++]));
                }
            }
        }
        #endregion
    }
}