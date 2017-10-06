using System;
using System.Collections.Concurrent;
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
        /// <summary>需要近实时保存的实体队列</summary>
        private ConcurrentDictionary<IEntity, IEntity> Entities { get; set; } = new ConcurrentDictionary<IEntity, IEntity>();

        /// <summary>需要延迟保存的实体队列</summary>
        private ConcurrentDictionary<IEntity, DateTime> DelayEntities { get; } = new ConcurrentDictionary<IEntity, DateTime>();

        /// <summary>调试开关，默认false</summary>
        public Boolean Debug { get; set; }

        /// <summary>数据访问</summary>
        public DAL Dal { get; set; }

        /// <summary>周期。默认1000毫秒，根据繁忙程度动态调节，尽量靠近每次持久化1000个对象</summary>
        public Int32 Period { get; set; } = 1000;

        /// <summary>完成事件。</summary>
        public event EventHandler<EventArgs<IEntity, Int32>> Completed;

        private TimerX _Timer;
        #endregion

        #region 构造
        /// <summary>实例化实体队列</summary>
        public EntityQueue()
        {
            _Timer = new TimerX(Work, null, Period, Period, "EQ") { Async = true };
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
                var dic = Entities;
                dic.TryAdd(entity, entity);
            }
            else
            {
                var dic = DelayEntities;
                //dic.AddOrUpdate(entity, TimerX.Now.AddMilliseconds(msDelay), (e, t) => t);
                dic.TryAdd(entity, TimerX.Now.AddMilliseconds(msDelay));
            }

            return true;
        }

        private void Work(Object state)
        {
            var list = new List<IEntity>();
            // 检查是否有延迟保存
            var dic = DelayEntities;
            if (!dic.IsEmpty)
            {
                var now = TimerX.Now;
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

            // 检查是否有近实时保存
            var es = Entities;
            if (!es.IsEmpty)
            {
                // 为了速度，不拷贝，直接创建一个新的集合
                Entities = new ConcurrentDictionary<IEntity, IEntity>();
                list.AddRange(es.Keys);
            }

            if (list.Count > 0) Process(list);
        }

        private void Process(Object state)
        {
            var list = state as ICollection<IEntity>;
            var dal = Dal;

            if (Debug || list.Count > 10000) XTrace.WriteLine("实体队列[{0}]\t准备持久化{1}个对象", dal.ConnName, list.Count);

            var rs = new List<Int32>();
            var sw = Stopwatch.StartNew();

            // 开启事务保存
            var useTrans = dal.DbType == DatabaseType.SQLite;
            if (useTrans) dal.BeginTransaction();
            try
            {
                foreach (var item in list)
                {
                    try
                    {
                        // 加入队列时已经Valid一次，这里不需要再次Valid
                        rs.Add(item.SaveWithoutValid());
                    }
                    catch { }
                }

                if (useTrans) dal.Commit();
            }
            catch
            {
                if (useTrans) dal.Rollback();
                throw;
            }
            finally
            {
                sw.Stop();
            }

            // 根据繁忙程度动态调节
            // 大于1000个对象时，说明需要加快持久化间隔，缩小周期
            // 小于1000个对象时，说明持久化太快了，加大周期
            var p = Period;
            if (list.Count > 1000)
                p = p * 1000 / list.Count;
            else
                p = p * 1000 / list.Count;

            // 最小间隔
            if (p < 500) p = 500;
            // 最大间隔
            if (p > 5000) p = 5000;

            if (p != Period)
            {
                Period = p;
                _Timer.Period = p;
            }

            if (Debug || list.Count > 10000)
            {
                var ms = sw.ElapsedMilliseconds;
                var speed = ms == 0 ? 0 : list.Count / ms;
                XTrace.WriteLine($"实体队列[{dal.ConnName}]\t耗时 {ms:n0}ms\t速度 {speed}tps\t周期 {p:n0}ms");
            }

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