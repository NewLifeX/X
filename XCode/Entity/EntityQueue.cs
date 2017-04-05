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
        /// <summary>需要近实时保存的实体队列</summary>
        private ICollection<IEntity> Entities { get; set; }

        /// <summary>需要延迟保存的实体队列</summary>
        private IDictionary<IEntity, DateTime> DelayEntities { get; } = new Dictionary<IEntity, DateTime>();

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
        /// <param name="entity">实体对象</param>
        /// <param name="msDelay">延迟保存的时间</param>
        /// <returns>返回是否添加成功，实体对象已存在于队列中则返回false</returns>
        public Boolean Add(IEntity entity, Int32 msDelay)
        {
            if (msDelay <= 0)
            {
                // 避免重复加入队列
                var es = Entities;
                if (es.Contains(entity)) return false;

                lock (this)
                {
                    es = Entities;
                    // 避免重复加入队列
                    if (es.Contains(entity)) return false;

                    es.Add(entity);
                }
            }
            else
            {
                var dic = DelayEntities;
                if (dic.ContainsKey(entity)) return false;

                lock (dic)
                {
                    if (dic.ContainsKey(entity)) return false;

                    dic[entity] = DateTime.Now.AddMilliseconds(msDelay);
                }
            }

            // 放到锁里面，避免重入
            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null) _Timer = new TimerX(Work, null, Period, Period, "EQ");
                }
            }

            return true;
        }

        private void Work(Object state)
        {
            //if (_Running) return;

            var list = new List<IEntity>();
            // 检查是否有延迟保存
            var dic = DelayEntities;
            if (dic.Count > 0)
            {
                lock (dic)
                {
                    var now = DateTime.Now;
                    foreach (var item in dic)
                    {
                        if (item.Value < now) list.Add(item.Key);
                    }
                    // 从列表删除过期
                    foreach (var item in list)
                    {
                        dic.Remove(item);
                    }
                }
            }

            // 检查是否有近实时保存
            var es = Entities;
            if (es.Count > 0)
            {
                lock (this)
                {
                    // 为了速度，不拷贝，直接创建一个新的集合
                    es = Entities;
                    if (es.Count > 0)
                    {
                        Entities = new HashSet<IEntity>();
                        list.AddRange(es);
                    }
                }
            }

            //_Running = true;
            //Task.Factory.StartNew(Process, list).LogException();
            if (list.Count > 0) Process(list);
        }

        //private Boolean _Running;
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
                    //rs.Add(item.Save());
                    // 加入队列时已经Valid一次，这里不需要再次Valid
                    rs.Add(item.SaveWithoutValid());
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
                //_Running = false;
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
                var k = 0;
                foreach (var item in list)
                {
                    Completed(this, new EventArgs<IEntity, Int32>(item, rs[k++]));
                }
            }
        }
        #endregion
    }
}